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

export function getVolume() {
    const audio = document.getElementById("global-audio")
    return audio.volume;
}

export function playWithFullLoad(src) {
    return new Promise((resolve, reject) => {
        const audio = document.getElementById("global-audio");

        pause();

        const xhr = new XMLHttpRequest();
        xhr.open('GET', src, true);
        xhr.responseType = 'blob';

        xhr.onprogress = function (e) {
            if (e.lengthComputable) {
                const percent = (e.loaded / e.total) * 100;

                if (window.dotNetAudioHelper && percent % 10 < 0.1) {
                    window.dotNetAudioHelper.invokeMethodAsync('OnLoadProgress', percent);
                }
            }
        };

        xhr.onload = function () {
            if (xhr.status === 200) {
                const blob = xhr.response;
                const url = URL.createObjectURL(blob);

                audio.src = url;
                audio.preload = "auto";

                audio.onloadedmetadata = () => {
                    audio.play()
                        .then(() => {
                            resolve(audio.duration);
                        })
                        .catch(e => {
                            console.error("Play failed:", e);
                            reject(e);
                        });
                };

                audio.onerror = (e) => {
                    console.error("Audio error after load:", e);
                    reject(e);
                };
            } else {
                reject(new Error(`Load failed: ${xhr.status}`));
            }
        };

        xhr.onerror = function () {
            reject(new Error("XHR error during load"));
        };

        xhr.send();
    });
}
