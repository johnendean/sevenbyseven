// The record detector for a Stack. Decides, frame by frame, when a record is being
// held up and captures it — see docs/adr/0005 for why this is a heuristic here in the
// browser rather than a call to a model.
//
// The reducer below was lifted unchanged from the prototype that validated it. It is
// pure: it takes the numbers measured from one frame plus the configured thresholds,
// and returns the next state and an effect for this file to carry out. Keep it that
// way — it is the part that was actually tested against real records.

import { start as startCamera, stopStream, toJpeg } from "./camera.js";

/** Working resolution. Small is fast, and less twitchy than full frames. */
const W = 160, H = 120;
const FPS = 15;

/** How fast the idle background creeps toward what it currently sees. */
const ADAPT = 0.08;

const Detector = (() => {

  const initial = {
    phase: 'calibrating',  // calibrating → empty → settling → captured → empty …
    stillFor: 0,           // consecutive frames held still
    emptyFor: 0,           // consecutive frames that count as cleared
    staleFor: 0,           // consecutive still frames of a non-record sitting in view
    captures: 0,
    capturedCoverage: 0,   // how full the frame was when we last fired
    note: 'Learning what an empty frame looks like. Hold nothing up.',
  };

  // Does what is in view look like a record — big enough, solid enough, busy enough?
  const looksLikeARecord = (m, t) =>
    m.coverage >= t.minCoverage && m.fill >= t.minFill && m.contrast >= t.minContrast;

  const isEmpty = (m, t) => m.coverage < t.emptyCoverage;

  // Far less in view than there was when we fired. Relative, so it survives the
  // baseline drifting upward between records.
  const hasGone = (s, m, t) =>
    m.coverage < Math.max(t.emptyCoverage, s.capturedCoverage * t.clearFraction);
  const isStill = (m, t) => m.motion < t.motionThreshold;

  /** (state, metrics, thresholds) → { state, effect } */
  function next(s, m, t) {
    const stay = (patch, effect = null) => ({ state: { ...s, ...patch }, effect });

    if (s.captures >= t.maxStack && s.phase !== 'full') {
      return stay({ phase: 'full', note: 'The Stack is full. Nothing more will be captured.' }, 'full');
    }
    if (s.phase === 'full') return stay({});

    switch (s.phase) {
      // Hold still with nothing in shot; that frame becomes the reference we
      // measure everything else against.
      case 'calibrating': {
        const stillFor = isStill(m, t) ? s.stillFor + 1 : 0;
        if (stillFor >= t.calibrateFrames) {
          return stay(
            { phase: 'empty', stillFor: 0, emptyFor: 0, note: 'Ready. Hold up a record.' },
            'learn-background');
        }
        return stay({ stillFor, note: 'Learning what an empty frame looks like. Hold nothing up.' });
      }

      // Nothing in view. Waiting for something to appear. While we wait, the idea
      // of "empty" is kept up to date, so a camera quietly re-exposing itself does
      // not slowly poison every later reading.
      case 'empty': {
        if (m.coverage >= t.minCoverage) {
          return stay({ phase: 'settling', stillFor: 0, staleFor: 0, note: 'Something is there. Hold it steady.' });
        }
        if (isStill(m, t)) {
          return stay({ note: 'Ready. Hold up a record.' }, 'adapt-background');
        }
        return stay({ note: 'Ready. Hold up a record.' });
      }

      // Something is in view. It has to look right AND stop moving.
      case 'settling': {
        if (isEmpty(m, t)) {
          return stay({ phase: 'empty', stillFor: 0, staleFor: 0, note: 'It went away again.' });
        }
        if (!looksLikeARecord(m, t)) {
          // In view, not a record, and not moving. That is not somebody presenting
          // something — that is the room. Absorb it rather than waiting on it.
          const staleFor = isStill(m, t) ? s.staleFor + 1 : 0;
          if (staleFor >= t.settleOutFrames) {
            return stay(
              { phase: 'empty', stillFor: 0, staleFor: 0, note: 'That is just part of the room now.' },
              'absorb');
          }
          return stay({ stillFor: 0, staleFor, note: 'In view, but it does not look like a record yet.' });
        }
        const stillFor = isStill(m, t) ? s.stillFor + 1 : 0;
        if (stillFor >= t.stillFrames) {
          return stay(
            { phase: 'captured', stillFor: 0, emptyFor: 0, staleFor: 0,
              captures: s.captures + 1, capturedCoverage: m.coverage,
              note: 'Captured. Now take it away.' },
            'capture');
        }
        return stay({ stillFor, staleFor: 0, note: isStill(m, t) ? 'Holding steady…' : 'Still moving.' });
      }

      // Already captured. Refuses to fire again until the record has gone — this is
      // what stops one record being captured twice.
      //
      // "Gone" is measured against how full the frame was when we fired, not against
      // an absolute figure. An absolute test cannot work: your arm is in shot, and the
      // camera re-exposes when a sleeve fills the lens, so the reading never returns
      // to where it started and the wait never ends.
      case 'captured': {
        const emptyFor = hasGone(s, m, t) ? s.emptyFor + 1 : 0;
        if (emptyFor >= t.emptyFrames) {
          return stay(
            { phase: 'empty', emptyFor: 0, staleFor: 0, note: 'Ready for the next one.' },
            'rearm');
        }
        return stay({ emptyFor, note: 'Take the record away before the next one.' });
      }
    }
    return stay({});
  }

  return { initial, next, looksLikeARecord, isEmpty, isStill, hasGone };
})();

function luminance(data) {
  const l = new Uint8ClampedArray(W * H);
  for (let i = 0, p = 0; i < data.length; i += 4, p++) {
    l[p] = (data[i] * 0.299 + data[i + 1] * 0.587 + data[i + 2] * 0.114) | 0;
  }
  return l;
}

/** Everything the pure module needs, measured off one frame. */
function measure(luma, t) {
    let motion = 0;
  if (prevLuma) {
    let sum = 0;
    for (let i = 0; i < luma.length; i++) sum += Math.abs(luma[i] - prevLuma[i]);
    motion = sum / luma.length;
  }

  if (!background) return { coverage: 0, fill: 0, contrast: 0, motion, box: null };

  // Foreground = whatever differs from the empty frame we learned earlier.
  const rows = new Int32Array(H), cols = new Int32Array(W);
  let count = 0;
  for (let y = 0; y < H; y++) {
    for (let x = 0; x < W; x++) {
      const i = y * W + x;
      if (Math.abs(luma[i] - background[i]) > t.fgDelta) { rows[y]++; cols[x]++; count++; }
    }
  }
  const coverage = count / (W * H);
  if (!count) return { coverage: 0, fill: 0, contrast: 0, motion, box: null };

  // Trim to where the foreground is actually dense, so a few stray pixels
  // cannot stretch the box across the whole frame.
  const edge = (arr, span, frac) => {
    const floor = span * frac;
    let lo = 0, hi = arr.length - 1;
    while (lo < arr.length && arr[lo] < floor) lo++;
    while (hi > lo && arr[hi] < floor) hi--;
    return [lo, hi];
  };
  const [y0, y1] = edge(rows, W, 0.04);
  const [x0, x1] = edge(cols, H, 0.04);
  const bw = Math.max(1, x1 - x0 + 1), bh = Math.max(1, y1 - y0 + 1);

  const fill = count / (bw * bh);

  let sum = 0, sum2 = 0, n = 0;
  for (let y = y0; y <= y1; y++) {
    for (let x = x0; x <= x1; x++) { const v = luma[y * W + x]; sum += v; sum2 += v * v; n++; }
  }
  const mean = sum / n;
  const contrast = Math.sqrt(Math.max(0, sum2 / n - mean * mean));

  return { coverage, fill, contrast, motion,
           box: { x: x0 / W, y: y0 / H, w: bw / W, h: bh / H } };
}


// ─────────────────────────────────────────────────────────────────────────────
// Everything below drives the reducer: the camera, the canvas, the overlay, and
// the one trip back to .NET that a capture costs.
//
// The loop deliberately does not report to .NET frame by frame. The highlight has
// to track a record in the hand, and fifteen interop calls a second over the
// circuit would neither keep up nor leave it usable. Only a capture and a change
// of phase cross the wire.
// ─────────────────────────────────────────────────────────────────────────────

const work = document.createElement("canvas");
work.width = W;
work.height = H;
const wctx = work.getContext("2d", { willReadFrequently: true });

let timer = null;
let video = null, box = null, badge = null, owner = null;
let thresholds = null, capture = null;
let background = null, prevLuma = null, state = null;
let capturing = false, paused = false;

export function probe() {
    return {
        hasMediaDevices: !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia),
        isSecureContext: window.isSecureContext === true,
    };
}

/**
 * Starts the camera and the detection loop.
 * @param options thresholds from ScanningOptions, plus maxDimension and jpegQuality.
 * @param dotNetRef receives OnCaptured and OnPhaseChanged.
 */
export async function start(videoEl, boxEl, badgeEl, options, dotNetRef) {
    await startCamera(videoEl);

    video = videoEl;
    box = boxEl;
    badge = badgeEl;
    owner = dotNetRef;
    thresholds = options;
    capture = { maxDimension: options.maxDimension, quality: options.jpegQuality };

    background = null;
    prevLuma = null;
    capturing = false;
    paused = false;
    state = { ...Detector.initial };

    stopTimer();
    timer = setInterval(tick, 1000 / FPS);
    paint({ box: null }, state);
}

export function stop() {
    stopTimer();
    stopStream();
    if (box) box.style.display = "none";
    owner = null;
}

/** Called when the Stack fills up: the camera stays on screen but nothing more is taken. */
export function pause() {
    paused = true;
    if (box) box.style.display = "none";
}

export function resume() {
    paused = false;
    // The scene has almost certainly moved on, so start from a fresh reference frame.
    background = null;
    state = { ...Detector.initial };
}

function stopTimer() {
    if (timer) {
        clearInterval(timer);
        timer = null;
    }
}

function tick() {
    if (paused || !video || !video.videoWidth) {
        return;
    }

    wctx.drawImage(video, 0, 0, W, H);
    const luma = luminance(wctx.getImageData(0, 0, W, H).data);
    const metrics = measure(luma, thresholds);

    const { state: nextState, effect } = Detector.next(state, metrics, thresholds);
    const phaseChanged = nextState.phase !== state.phase;
    state = nextState;

    switch (effect) {
        case "learn-background":
        case "rearm":
        case "absorb":
            background = Float32Array.from(luma);
            break;
        case "adapt-background":
            if (background) {
                for (let i = 0; i < luma.length; i++) {
                    background[i] += (luma[i] - background[i]) * ADAPT;
                }
            }
            break;
        case "capture":
            send();
            break;
    }

    prevLuma = luma;
    paint(metrics, state);

    if (phaseChanged && owner) {
        owner.invokeMethodAsync("OnPhaseChanged", state.phase, state.note).catch(() => { });
    }
}

/** Pulls a full-resolution still and hands it to .NET. */
async function send() {
    if (capturing || !owner) {
        return;
    }

    capturing = true;
    flash();

    try {
        const jpeg = await toJpeg(
            video, video.videoWidth, video.videoHeight, capture.maxDimension, capture.quality);
        await owner.invokeMethodAsync("OnCaptured", jpeg);
    } catch {
        // A capture that does not make it is not worth stopping the Stack for; the
        // record is still in your hand, and taking it away and back tries again.
        if (owner) {
            owner.invokeMethodAsync("OnCaptureFailed").catch(() => { });
        }
    } finally {
        capturing = false;
    }
}

function paint(metrics, s) {
    if (badge) {
        badge.textContent = s.note;
    }

    if (!box) {
        return;
    }

    const visible = metrics.box
        && s.phase !== "calibrating"
        && metrics.coverage >= thresholds.emptyCoverage;

    if (!visible) {
        box.style.display = "none";
        return;
    }

    const b = metrics.box;
    box.style.display = "block";
    box.style.left = b.x * 100 + "%";
    box.style.top = b.y * 100 + "%";
    box.style.width = b.w * 100 + "%";
    box.style.height = b.h * 100 + "%";
    box.classList.toggle("locked", Detector.looksLikeARecord(metrics, thresholds));
}

/** The one cue that works when you are looking at the record and not the screen. */
function flash() {
    if (!video || !video.parentElement) {
        return;
    }

    video.parentElement.classList.add("flash");
    setTimeout(() => video.parentElement?.classList.remove("flash"), 110);
}
