// Per-device conveniences, held in this browser only. Storage can be missing or refuse
// (a private window, cleared site data), so a read comes back null and a write is
// dropped rather than either one throwing: the page falls back to its default.

export function read(key) {
    try {
        return window.localStorage.getItem(key);
    } catch {
        return null;
    }
}

export function write(key, value) {
    try {
        window.localStorage.setItem(key, value);
    } catch {
        // Nothing to remember it in. The next visit starts from the default.
    }
}
