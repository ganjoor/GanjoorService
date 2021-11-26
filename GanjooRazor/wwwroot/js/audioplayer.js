//original source: https://codepen.io/craigstroman/pen/aOyRYx
var audioPlayer = function () {
    "use strict";

    var verseStart = [];
    var verseEnd = [];
    var verseIndex = [];
    var nLastHighlight = -1;
    var vCount = 0;

    // Private variables
    var _currentTrack = null;
    var _elements = {
        audio: document.getElementById("audio"),
        playerButtons: {
            largeToggleBtn: document.querySelector(".large-toggle-btn"),
            nextTrackBtn: document.querySelector(".next-track-btn"),
            previousTrackBtn: document.querySelector(".previous-track-btn"),
            smallToggleBtn: document.getElementsByClassName("small-toggle-btn")
        },
        progressBar: document.querySelector(".progress-box"),
        playListRows: document.getElementsByClassName("play-list-row"),
        trackInfoBox: document.querySelector(".track-info-box")
    };
    var _playAHead = false;
    var _progressBarIndicator = _elements.progressBar.children[0].children[0].children[1];
    var _trackLoaded = false;

    /**
     * Determines the buffer progress.
     *
     * @param audio The audio element on the page.
     **/
    var _bufferProgress = function (audio) {
        var bufferedTime = (audio.buffered.end(0) * 100) / audio.duration;
        var progressBuffer = _elements.progressBar.children[0].children[0].children[0];

        progressBuffer.style.width = bufferedTime + "%";
    };

    /**
     * A utility function for getting the event cordinates based on browser type.
     *
     * @param e The JavaScript event.
     **/
    var _getXY = function (e) {
        var containerX = _elements.progressBar.offsetLeft;
        var containerY = _elements.progressBar.offsetTop;

        var coords = {
            x: null,
            y: null
        };

        var isTouchSuopported = "ontouchstart" in window;

        if (isTouchSuopported) { //For touch devices
            coords.x = e.clientX - containerX;
            coords.y = e.clientY - containerY;

            return coords;
        } else if (e.offsetX || e.offsetX === 0) { // For webkit browsers
            coords.x = e.offsetX;
            coords.y = e.offsetY;

            return coords;
        } else if (e.layerX || e.layerX === 0) { // For Mozilla firefox
            coords.x = e.layerX;
            coords.y = e.layerY;

            return coords;
        }
    };

    var _handleProgressIndicatorClick = function (e) {
        var progressBar = document.querySelector(".progress-box");
        var xCoords = _getXY(e).x;

        return (xCoords - progressBar.offsetLeft) / progressBar.children[0].offsetWidth;
    };

    /**
     * Initializes the html5 audio player and the playlist.
     *
     **/
    var initPlayer = function () {

        if (_currentTrack === 1 || _currentTrack === null) {
            _elements.playerButtons.previousTrackBtn.disabled = true;
        }

        //Adding event listeners to playlist clickable elements.
        for (var i = 0; i < _elements.playListRows.length; i++) {
            var smallToggleBtn = _elements.playerButtons.smallToggleBtn[i];
            var playListLink = _elements.playListRows[i].children[2].children[0];

            //Playlist link clicked.
            playListLink.addEventListener("click", function (e) {
                e.preventDefault();
                var selectedTrack = parseInt(this.parentNode.parentNode.getAttribute("data-track-row"));

                if (selectedTrack !== _currentTrack) {
                    _resetPlayStatus();
                    _currentTrack = null;
                    _trackLoaded = false;
                }

                if (_trackLoaded === false) {
                    _currentTrack = parseInt(selectedTrack);
                    _setTrack();
                } else {
                    _playBack(this);
                }
            }, false);

            //Small toggle button clicked.
            smallToggleBtn.addEventListener("click", function (e) {
                e.preventDefault();
                var selectedTrack = parseInt(this.parentNode.getAttribute("data-track-row"));

                if (selectedTrack !== _currentTrack) {
                    _resetPlayStatus();
                    _currentTrack = null;
                    _trackLoaded = false;
                }

                if (_trackLoaded === false) {
                    _currentTrack = parseInt(selectedTrack);
                    _setTrack();
                } else {
                    _playBack(this);
                }

            }, false);
        }

        //Audio time has changed so update it.
        _elements.audio.addEventListener("timeupdate", _trackTimeChanged, false);

        //Audio track has ended playing.
        _elements.audio.addEventListener("ended", function (e) {
            _trackHasEnded();
        }, false);

        //Audio error. 
        _elements.audio.addEventListener("error", function (e) {
            switch (e.target.error.code) {
                case e.target.error.MEDIA_ERR_ABORTED:
                    alert('You aborted the video playback.');
                    break;
                case e.target.error.MEDIA_ERR_NETWORK:
                    alert('A network error caused the audio download to fail.');
                    break;
                case e.target.error.MEDIA_ERR_DECODE:
                    alert('The audio playback was aborted due to a corruption problem or because the video used features your browser did not support.');
                    break;
                case e.target.error.MEDIA_ERR_SRC_NOT_SUPPORTED:
                    alert('The video audio not be loaded, either because the server or network failed or because the format is not supported.');
                    break;
                default:
                    alert('An unknown error occurred.');
                    break;
            }
            trackLoaded = false;
            _resetPlayStatus();
        }, false);

        //Large toggle button clicked.
        _elements.playerButtons.largeToggleBtn.addEventListener("click", function (e) {
            if (_trackLoaded === false) {
                _currentTrack = parseInt(1);
                _setTrack()
            } else {
                _playBack();
            }
        }, false);

        //Next track button clicked.
        _elements.playerButtons.nextTrackBtn.addEventListener("click", function (e) {
            if (this.disabled !== true) {
                _currentTrack++;
                _trackLoaded = false;
                _resetPlayStatus();
                _setTrack();
            }
        }, false);

        //Previous track button clicked.
        _elements.playerButtons.previousTrackBtn.addEventListener("click", function (e) {
            if (this.disabled !== true) {
                _currentTrack--;
                _trackLoaded = false;
                _resetPlayStatus();
                _setTrack();
            }
        }, false);

        //User is moving progress indicator.
        _progressBarIndicator.addEventListener("mousedown", _mouseDown, false);

        //User stops moving progress indicator.
        window.addEventListener("mouseup", _mouseUp, false);
    };

    /**
     * Handles the mousedown event by a user and determines if the mouse is being moved.
     *
     * @param e The event object.
     **/
    var _mouseDown = function (e) {
        window.addEventListener("mousemove", _moveProgressIndicator, true);
        audio.removeEventListener("timeupdate", _trackTimeChanged, false);

        _playAHead = true;
    };

    /**
     * Handles the mouseup event by a user.
     *
     * @param e The event object.
     **/
    var _mouseUp = function (e) {
        if (_playAHead === true) {
            var duration = parseFloat(audio.duration);
            var progressIndicatorClick = parseFloat(_handleProgressIndicatorClick(e));
            window.removeEventListener("mousemove", _moveProgressIndicator, true);

            audio.currentTime = duration * progressIndicatorClick;
            audio.addEventListener("timeupdate", _trackTimeChanged, false);
            _playAHead = false;
        }
    };

    /**
     * Moves the progress indicator to a new point in the audio.
     *
     * @param e The event object.
     **/
    var _moveProgressIndicator = function (e) {
        var newPosition = 0;
        var progressBarOffsetLeft = _elements.progressBar.offsetLeft;
        var progressBarWidth = 0;
        var progressBarIndicator = _elements.progressBar.children[0].children[0].children[1];
        var progressBarIndicatorWidth = _progressBarIndicator.offsetWidth;
        var xCoords = _getXY(e).x;

        progressBarWidth = _elements.progressBar.children[0].offsetWidth - progressBarIndicatorWidth;
        newPosition = xCoords - progressBarOffsetLeft;

        if ((newPosition >= 1) && (newPosition <= progressBarWidth)) {
            progressBarIndicator.style.left = newPosition + ".px";
        }
        if (newPosition < 0) {
            progressBarIndicator.style.left = "0";
        }
        if (newPosition > progressBarWidth) {
            progressBarIndicator.style.left = progressBarWidth + "px";
        }
    };

    /**
     * Controls playback of the audio element.
     *
     **/
    var _playBack = function () {
        if (_elements.audio.paused) {
            _elements.audio.play();
            _updatePlayStatus(true);
        } else {
            _elements.audio.pause();
            _updatePlayStatus(false);
        }
    };

    var _setXml = function () {
        var xmlUrl = _elements.playListRows[_currentTrack - 1].children[3].innerHTML;
        $.ajax({
            type: "GET",
            url: xmlUrl,
            dataType: "xml",
            success: function (xml) {
                var nOneSecondBugFix = 2000;
                $(xml).find('OneSecondBugFix').each(function () {
                    nOneSecondBugFix = parseInt($(xml).find('OneSecondBugFix').text());
                });
                var v = 0;
                $(xml).find('SyncInfo').each(function () {
                    verseStart[v] = parseInt($(this).find('AudioMiliseconds').text()) / nOneSecondBugFix;
                    verseIndex[v] = parseInt($(this).find('VerseOrder').text());
                    if (v > 0)
                        verseEnd[v - 1] = verseStart[v];
                    v++;
                });
                v--;
                if (v > 1)
                    verseEnd[v] = verseStart[v] + 2 * (verseEnd[v - 1] - verseStart[v - 1]);
                vCount = v;
            }
        });
    }

    /**
     * Sets the track if it hasn't already been loaded yet.
     *
     **/
    var _setTrack = function () {
        _setXml();
        var songURL = _elements.audio.children[_currentTrack - 1].src;

        _elements.audio.setAttribute("src", songURL);
        _elements.audio.load();

        _trackLoaded = true;

        _setTrackTitle(_currentTrack, _elements.playListRows);

        _setActiveItem(_currentTrack, _elements.playListRows);

        _elements.trackInfoBox.style.visibility = "visible";

        _playBack();
    };

    /**
     * Sets the activly playing item within the playlist.
     *
     * @param currentTrack The current track number being played.
     * @param playListRows The playlist object.
     **/
    var _setActiveItem = function (currentTrack, playListRows) {
        for (var i = 0; i < playListRows.length; i++) {
            playListRows[i].children[2].className = "track-title";
        }

        playListRows[currentTrack - 1].children[2].className = "track-title active-track";
    };

    /**
     * Sets the text for the currently playing song.
     *
     * @param currentTrack The current track number being played.
     * @param playListRows The playlist object.
     **/
    var _setTrackTitle = function (currentTrack, playListRows) {
        var trackTitleBox = document.querySelector(".player .info-box .track-info-box .track-title-text");
        var trackTitle = playListRows[currentTrack - 1].children[2].innerHTML;

        trackTitleBox.innerHTML = null;

        trackTitleBox.innerHTML = trackTitle;

    };

    /**
     * Plays the next track when a track has ended playing.
     *
     **/
    var _trackHasEnded = function () {
        parseInt(_currentTrack);
        _currentTrack = (_currentTrack === _elements.playListRows.length) ? 1 : _currentTrack + 1;
        _trackLoaded = false;

        _resetPlayStatus();

        _setTrack();
    };

    /**
     * Updates the time for the song being played.
     *
     **/
    var _trackTimeChanged = function () {
        var currentTimeBox = document.querySelector(".player .info-box .track-info-box .audio-time .current-time");
        var currentTime = audio.currentTime;
        var duration = audio.duration;
        var durationBox = document.querySelector(".player .info-box .track-info-box .audio-time .duration");
        var trackCurrentTime = _trackTime(currentTime);
        var trackDuration = _trackTime(duration);

        currentTimeBox.innerHTML = null;
        currentTimeBox.innerHTML = trackCurrentTime;

        durationBox.innerHTML = null;
        durationBox.innerHTML = trackDuration;

        _updateProgressIndicator(audio);
        _bufferProgress(audio);

        if (currentTime > 0) {
            for (var i = 0; i <= vCount; i++) {
                if (currentTime >= verseStart[i] && currentTime <= verseEnd[i]) {
                    hilightverse(verseIndex[i], "red", true, playerScrollLock);

                    if (nLastHighlight != verseIndex[i] && nLastHighlight != -1)
                        hilightverse(nLastHighlight, "black", false, false);
                    nLastHighlight = verseIndex[i];
                    break;
                }
            }
        }
    };

    /**
     * A utility function for converting a time in miliseconds to a readable time of minutes and seconds.
     *
     * @param seconds The time in seconds.
     *
     * @return time The time in minutes and/or seconds.
     **/
    var _trackTime = function (seconds) {
        var min = 0;
        var sec = Math.floor(seconds);
        var time = 0;

        min = Math.floor(sec / 60);

        min = min >= 10 ? min : '0' + min;

        sec = Math.floor(sec % 60);

        sec = sec >= 10 ? sec : '0' + sec;

        time = min + ':' + sec;

        return time;
    };

    /**
     * Updates both the large and small toggle buttons accordingly.
     *
     * @param audioPlaying A booean value indicating if audio is playing or paused.
     **/
    var _updatePlayStatus = function (audioPlaying) {
        if (audioPlaying) {
            _elements.playerButtons.largeToggleBtn.children[0].className = "large-pause-btn";

            _elements.playerButtons.smallToggleBtn[_currentTrack - 1].children[0].className = "small-pause-btn";
        } else {
            _elements.playerButtons.largeToggleBtn.children[0].className = "large-play-btn";

            _elements.playerButtons.smallToggleBtn[_currentTrack - 1].children[0].className = "small-play-btn";
        }

        //Update next and previous buttons accordingly
        if (_currentTrack === 1) {
            _elements.playerButtons.previousTrackBtn.disabled = true;
            _elements.playerButtons.previousTrackBtn.className = "previous-track-btn disabled";
        } else if (_currentTrack > 1 && _currentTrack !== _elements.playListRows.length) {
            _elements.playerButtons.previousTrackBtn.disabled = false;
            _elements.playerButtons.previousTrackBtn.className = "previous-track-btn";
            _elements.playerButtons.nextTrackBtn.disabled = false;
            _elements.playerButtons.nextTrackBtn.className = "next-track-btn";
        } else if (_currentTrack === _elements.playListRows.length) {
            _elements.playerButtons.nextTrackBtn.disabled = true;
            _elements.playerButtons.nextTrackBtn.className = "next-track-btn disabled";
        }
    };

    /**
     * Updates the location of the progress indicator according to how much time left in audio.
     *
     **/
    var _updateProgressIndicator = function () {
        var currentTime = parseFloat(_elements.audio.currentTime);
        var duration = parseFloat(_elements.audio.duration);
        var indicatorLocation = 0;
        var progressBarWidth = parseFloat(_elements.progressBar.offsetWidth);
        var progressIndicatorWidth = parseFloat(_progressBarIndicator.offsetWidth);
        var progressBarIndicatorWidth = progressBarWidth - progressIndicatorWidth;

        indicatorLocation = progressBarIndicatorWidth * (currentTime / duration);

        _progressBarIndicator.style.left = indicatorLocation + "px";
    };

    /**
     * Resets all toggle buttons to be play buttons.
     *
     **/
    var _resetPlayStatus = function () {
        var smallToggleBtn = _elements.playerButtons.smallToggleBtn;

        _elements.playerButtons.largeToggleBtn.children[0].className = "large-play-btn";

        for (var i = 0; i < smallToggleBtn.length; i++) {
            if (smallToggleBtn[i].children[0].className === "small-pause-btn") {
                smallToggleBtn[i].children[0].className = "small-play-btn";
            }
        }
    };

    return {
        initPlayer: initPlayer
    };
};

(function () {
    var player = new audioPlayer();

    player.initPlayer();
})();