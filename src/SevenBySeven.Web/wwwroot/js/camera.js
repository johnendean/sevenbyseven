// Camera capture for the Scanning slice. Photographs are downscaled here, in the
// browser, so a 12MB phone photo never travels whole.

import { read, write } from "./preferences.js";

/** The camera last chosen on this device, shared by a single Scan and a Stack. */
const CameraKey = "sevenbyseven.camera";

let stream = null;
let video = null;

/** The device the stream is running on, as the browser reported it. */
let current = null;

/** Told the camera list and the current camera whenever either may have changed. */
const watchers = new Set();

/** Told whenever the stream moves to another camera, so whatever reads frames can start afresh. */
const switchListeners = new Set();

export function probe() {
    return {
        hasMediaDevices: !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia),
        // getUserMedia is unavailable outside a secure context, which includes a phone
        // hitting a dev machine by IP over plain HTTP.
        isSecureContext: window.isSecureContext === true,
    };
}

/**
 * Starts the camera remembered on this device, or the rear-facing one when nothing is
 * remembered or the remembered one is no longer there.
 */
export async function start(videoEl) {
    stopStream();
    video = videoEl;

    const remembered = read(CameraKey);
    if (remembered) {
        try {
            await open(remembered);
            return;
        } catch (error) {
            // Declining permission is not the camera being gone; trying another will only ask again.
            if (error?.name === "NotAllowedError") {
                throw error;
            }
        }
    }

    await open(null);
}

/**
 * Moves the stream to another camera and remembers it. One that will not start leaves
 * the previous camera running and is not remembered; the error goes back to the caller.
 */
export async function switchTo(deviceId) {
    if (!video || deviceId === current) {
        return;
    }

    const previous = current;
    stopTracks();

    try {
        await open(deviceId);
    } catch (error) {
        try {
            await open(previous);
        } catch {
            await fallBack();
        }
        throw error;
    }

    write(CameraKey, deviceId);
}

export function stopStream() {
    stopTracks();
    video = null;
    current = null;
    notify();
}

/**
 * Keeps a picker told of the cameras on offer. It hears straight away if a camera is
 * already running, and again whenever one starts, switches or is plugged in or out.
 */
export function watch(dotNetRef) {
    watchers.add(dotNetRef);
    notify();
}

export function unwatch(dotNetRef) {
    watchers.delete(dotNetRef);
}

/** Calls back whenever the stream moves to another camera. Returns the way to stop listening. */
export function onSwitched(callback) {
    switchListeners.add(callback);
    return () => switchListeners.delete(callback);
}

/** A null deviceId asks for the rear-facing camera, or whatever the browser offers instead. */
async function open(deviceId) {
    const camera = deviceId ? { deviceId: { exact: deviceId } } : { facingMode: { ideal: "environment" } };

    stream = await navigator.mediaDevices.getUserMedia({
        video: {
            ...camera,
            width: { ideal: 1920 },
            height: { ideal: 1920 },
        },
        audio: false,
    });

    const previous = current;
    current = stream.getVideoTracks()[0]?.getSettings().deviceId ?? deviceId;

    video.srcObject = stream;
    await video.play();

    if (previous && previous !== current) {
        for (const listener of switchListeners) {
            listener();
        }
    }

    notify();
}

/** The default camera, or none at all when even that will not start. */
async function fallBack() {
    try {
        await open(null);
    } catch {
        current = null;
        notify();
    }
}

function stopTracks() {
    if (!stream) {
        return;
    }
    for (const track of stream.getTracks()) {
        track.stop();
    }
    stream = null;
}

/** Labels are blank until permission is granted, so a blank one is numbered instead. */
async function cameras() {
    const devices = await navigator.mediaDevices.enumerateDevices();
    return devices
        .filter(device => device.kind === "videoinput")
        .map((device, index) => ({ id: device.deviceId, label: device.label || `Camera ${index + 1}` }));
}

async function notify() {
    if (watchers.size === 0) {
        return;
    }

    let list = [];
    try {
        list = stream ? await cameras() : [];
    } catch {
        // No list to offer; the picker hides itself.
    }

    for (const watcher of watchers) {
        watcher.invokeMethodAsync("OnCamerasChanged", list, current).catch(() => { });
    }
}

/**
 * A camera plugged in or out. One that took the running stream with it falls back to the
 * default camera, without forgetting the one remembered — it may well be plugged back in.
 */
async function onDeviceChange() {
    if (stream && current) {
        try {
            const list = await cameras();
            if (!list.some(camera => camera.id === current)) {
                stopTracks();
                await fallBack();
                return;
            }
        } catch {
            // No list to check against; leave the stream as it is.
        }
    }

    notify();
}

navigator.mediaDevices?.addEventListener?.("devicechange", onDeviceChange);

export function toJpeg(source, sourceWidth, sourceHeight, maxDimension, quality) {
    const scale = Math.min(1, maxDimension / Math.max(sourceWidth, sourceHeight));
    const canvas = document.createElement("canvas");
    canvas.width = Math.max(1, Math.round(sourceWidth * scale));
    canvas.height = Math.max(1, Math.round(sourceHeight * scale));

    const context = canvas.getContext("2d");
    context.drawImage(source, 0, 0, canvas.width, canvas.height);

    return new Promise((resolve, reject) => {
        canvas.toBlob(
            blob => blob
                ? resolve({
                    // The JPEG is handed over as a stream reference rather than inside the
                    // interop result. An interop result is a single SignalR message, and the
                    // circuit's receive limit is 32kB by default — a sleeve photograph is an
                    // order of magnitude past that, and exceeding it drops the connection
                    // silently mid-capture. A stream reference is chunked under the limit.
                    data: DotNet.createJSStreamReference(blob),
                    width: canvas.width,
                    height: canvas.height,
                })
                : reject(new Error("The canvas produced no image.")),
            "image/jpeg",
            quality);
    });
}

export function captureFrame(video, maxDimension, quality) {
    if (!video || !video.videoWidth || !video.videoHeight) {
        return null;
    }
    return toJpeg(video, video.videoWidth, video.videoHeight, maxDimension, quality);
}

export async function captureFile(input, maxDimension, quality) {
    const file = input?.files?.[0];
    if (!file) {
        return null;
    }

    const bitmap = await createImageBitmap(file);
    try {
        return await toJpeg(bitmap, bitmap.width, bitmap.height, maxDimension, quality);
    } finally {
        bitmap.close();
    }
}

export function clearFile(input) {
    if (input) {
        input.value = "";
    }
}
