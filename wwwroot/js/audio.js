export function play(src) {
    const audio = document.getElementById("global-audio");
    audio.src = src;
    audio.play();

    return new Promise(resolve => {
        audio.onloadedmetadata = () => {
            resolve(audio.duration)
        }
    })
}

export function pause() {
    const audio = document.getElementById("global-audio");
    audio.pause();
}

export function resume() {
    const audio = document.getElementById("global-audio");
    audio.play();
}

export function setVolume(v) {
    const audio = document.getElementById("global-audio");
    audio.volume = v;
}

export function setTime(s) {
    const audio = document.getElementById("global-audio")
    audio.currentTime = s;
}

export function getCurrentTime() {
    const audio = document.getElementById("global-audio")
    return audio.currentTime;
}

export function getDuration() {
    const audio = document.getElementById("global-audio")
    return audio.duration;
}

export function isPaused() {
    const audio = document.getElementById("global-audio")
    return audio.paused;
}
