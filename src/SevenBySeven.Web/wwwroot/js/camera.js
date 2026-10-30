// Camera capture for the Scanning slice. Photographs are downscaled here, in the
// browser, so a 12MB phone photo never travels whole.

import { read, write } from "./preferences.js";

/** The camera last chosen on this device, shared by a single Scan and a Stack. */
const CameraKey = "sevenbyseven.camera";

let stream = null;
let video = null;

/** The device the stream is running on, as the browser reported it. */
let current = null;

/**
 * Moved on by every change of camera, and by stopping it. Only the latest change may
 * take the stream: one overtaken while its camera was opening — by a stop, a newer
 * start, a switch or an unplugging — shuts that camera straight back off and is
 * Superseded, whether the camera opened or failed.
 */
let generation = 0;

/** The changes of camera, one at a time: some phones cannot open two cameras at once. */
let queue = Promise.resolve();

/** Told the camera list and the current camera whenever either may have changed, by token. */
const watchers = new Map();
let nextWatcher = 0;

/** Told whenever the stream moves to another camera, so whatever reads frames can start afresh. */
const switchListeners = new Set();

/** A change of camera overtaken by a later one. Nothing to report or recover. */
class Superseded extends Error {
    name = "Superseded";
}

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
 * remembered or the remembered one is no longer there. False when the start was
 * overtaken — the camera stopped, or started again — before it finished.
 */
export async function start(videoEl) {
    stopStream();
    video = videoEl;

    return settled(change(async mine => {
        const remembered = read(CameraKey);
        if (remembered) {
            try {
                await open(remembered, mine);
                return;
            } catch (error) {
                // Declining permission is not the camera being gone; trying another will only ask again.
                if (error instanceof Superseded || error?.name === "NotAllowedError") {
                    throw error;
                }
            }
        }

        await open(null, mine);
    }));
}

/**
 * Moves the stream to another camera and remembers it. One that will not start leaves
 * the previous camera running and is not remembered; the error goes back to the caller.
 */
export async function switchTo(deviceId) {
    if (!video || deviceId === current) {
        return;
    }

    await settled(change(async mine => {
        const previous = current;

        try {
            await open(deviceId, mine);
        } catch (error) {
            if (error instanceof Superseded) {
                throw error;
            }
            try {
                await open(previous, mine);
            } catch (retry) {
                if (retry instanceof Superseded) {
                    throw retry;
                }
                await fallBack(mine);
            }
            throw error;
        }

        write(CameraKey, deviceId);
    }));
}

export function stopStream() {
    generation++;
    stopTracks();
    video = null;
    current = null;
    notify();
}

/**
 * Keeps a picker told of the cameras on offer. It hears straight away if a camera is
 * already running, and again whenever one starts, switches or is plugged in or out.
 * Returns the token to stop with: a .NET reference passed in twice arrives as two
 * different objects, so it cannot identify the watcher itself.
 */
export function watch(dotNetRef) {
    const token = ++nextWatcher;
    watchers.set(token, dotNetRef);
    notify();
    return token;
}

export function unwatch(token) {
    watchers.delete(token);
}

/** Calls back whenever the stream moves to another camera. Returns the way to stop listening. */
export function onSwitched(callback) {
    switchListeners.add(callback);
    return () => switchListeners.delete(callback);
}

/**
 * Queues a change of camera behind any already running, and overtakes every one still
 * waiting or opening. The change is handed its generation to open cameras under.
 */
function change(run) {
    const mine = ++generation;
    const next = queue.then(() => {
        if (mine !== generation) {
            throw new Superseded();
        }
        return run(mine);
    });
    queue = next.catch(() => { });
    return next;
}

/** True once a change has gone through, false when it was overtaken; any other failure is thrown. */
async function settled(pending) {
    try {
        await pending;
        return true;
    } catch (error) {
        if (error instanceof Superseded) {
            return false;
        }
        throw error;
    }
}

/** A null deviceId asks for the rear-facing camera, or whatever the browser offers instead. */
async function open(deviceId, mine) {
    const camera = deviceId ? { deviceId: { exact: deviceId } } : { facingMode: { ideal: "environment" } };

    // Let go of the running camera first, as a phone may not open a second one alongside it.
    stopTracks();

    let opened;
    try {
        opened = await navigator.mediaDevices.getUserMedia({
            video: {
                ...camera,
                width: { ideal: 1920 },
                height: { ideal: 1920 },
            },
            audio: false,
        });
    } catch (error) {
        throw mine === generation ? error : new Superseded();
    }

    if (mine !== generation) {
        for (const track of opened.getTracks()) {
            track.stop();
        }
        throw new Superseded();
    }

    stream = opened;
    const previous = current;
    current = stream.getVideoTracks()[0]?.getSettings().deviceId ?? deviceId;

    video.srcObject = stream;
    try {
        await video.play();
    } catch (error) {
        // A stop while it played has shut this stream off and interrupted the play.
        throw mine === generation ? error : new Superseded();
    }

    if (mine !== generation) {
        throw new Superseded();
    }

    if (previous && previous !== current) {
        for (const listener of switchListeners) {
            listener();
        }
    }

    notify();
}

/** The default camera, or none at all when even that will not start. */
async function fallBack(mine) {
    try {
        await open(null, mine);
    } catch (error) {
        if (error instanceof Superseded) {
            throw error;
        }
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

    for (const watcher of watchers.values()) {
        watcher.invokeMethodAsync("OnCamerasChanged", list, current).catch(() => { });
    }
}

/**
 * A camera plugged in or out. One that took the running stream with it falls back to the
 * default camera, without forgetting the one remembered — it may well be plugged back in.
 * Anything else that changed the camera while the list was read has the last word.
 */
async function onDeviceChange() {
    const seen = generation;

    if (stream && current) {
        try {
            const list = await cameras();
            if (seen !== generation) {
                return;
            }
            if (!list.some(camera => camera.id === current)) {
                await settled(change(fallBack));
                return;
            }
        } catch {
            // No list to check against, or the default would not start; the picker hears below.
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
