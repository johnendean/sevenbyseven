// Camera capture for the Scanning slice. Photographs are downscaled here, in the
// browser, so a 12MB phone photo never travels whole.

let stream = null;

export function probe() {
    return {
        hasMediaDevices: !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia),
        // getUserMedia is unavailable outside a secure context, which includes a phone
        // hitting a dev machine by IP over plain HTTP.
        isSecureContext: window.isSecureContext === true,
    };
}

export async function start(video) {
    stopStream();
    stream = await navigator.mediaDevices.getUserMedia({
        video: {
            facingMode: { ideal: "environment" },
            width: { ideal: 1920 },
            height: { ideal: 1920 },
        },
        audio: false,
    });
    video.srcObject = stream;
    await video.play();
}

export function stopStream() {
    if (!stream) {
        return;
    }
    for (const track of stream.getTracks()) {
        track.stop();
    }
    stream = null;
}

function toJpeg(source, sourceWidth, sourceHeight, maxDimension, quality) {
    const scale = Math.min(1, maxDimension / Math.max(sourceWidth, sourceHeight));
    const canvas = document.createElement("canvas");
    canvas.width = Math.max(1, Math.round(sourceWidth * scale));
    canvas.height = Math.max(1, Math.round(sourceHeight * scale));

    const context = canvas.getContext("2d");
    context.drawImage(source, 0, 0, canvas.width, canvas.height);

    return {
        dataUrl: canvas.toDataURL("image/jpeg", quality),
        width: canvas.width,
        height: canvas.height,
    };
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
        return toJpeg(bitmap, bitmap.width, bitmap.height, maxDimension, quality);
    } finally {
        bitmap.close();
    }
}

export function clearFile(input) {
    if (input) {
        input.value = "";
    }
}
