var audioxmlfiles = [];
var narrators = [];
var jlist;
function addpaudio(index, jplaylist, xmlfilename, poemtitle, auartist, oggurl, mp3url) {
    audioxmlfiles[index] = xmlfilename;
    narrators[index] = auartist;
    jplaylist.add({
        title: poemtitle,
        artist: auartist,
        mp3: mp3url
    });
}
function prepaudio(xmlfilename, poemtitle, auartist, oggurl, mp3url) {
    var verseStart = [];
    var verseEnd = [];
    var verseIndex = [];
    var nLastHighlight = -1;
    var vCount = 0;

    audioxmlfiles[0] = xmlfilename;
    narrators[0] = auartist;


    jlist = new jPlayerPlaylist({
        jPlayer: "#jquery_jplayer_1",
        cssSelectorAncestor: "#jp_container_1"
    }, [
        {
            title: poemtitle,
            artist: auartist,
            mp3: mp3url
        },
    ],
        {
            setmedia: function (event) {

                $.ajax({
                    type: "GET",
                    url: audioxmlfiles[jlist.current],
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


            },

            timeupdate: function (event) { // 4Hz
                var curTime = event.jPlayer.status.currentTime;
                if (curTime > 0) {
                    for (i = 0; i <= vCount; i++) {
                        if (curTime >= verseStart[i] && curTime <= verseEnd[i]) {
                            hilightverse(verseIndex[i], "red", true, playerScrollLock);

                            if (nLastHighlight != verseIndex[i] && nLastHighlight != -1)
                                hilightverse(nLastHighlight, "black", false, false);
                            nLastHighlight = verseIndex[i];
                            break;
                        }
                    }
                }

            },
            ended: function (event) { // 4Hz
                if (nLastHighlight != -1)
                    hilightverse(nLastHighlight, "black", false, false);
            },
            swfPath: "dist/jplayer",
            supplied: "oga, mp3",
            wmode: "window",
            useStateClassSkin: true,
            autoBlur: false,
            smoothPlayBar: true,
            keyEnabled: true,
            remainingDuration: true,
            toggleDuration: true
        });

    return jlist;

}
