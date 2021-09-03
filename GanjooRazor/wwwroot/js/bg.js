﻿// From David Flanagan's "JavaScript: The Definitive Guide" 5th Ed,
//   http://www.davidflanagan.com/javascript5/display.php?n=15-4&f=15/04.js
//modified 4 ganjoor a little bit

function isMember(element, classname) {
    var classes = element.className;
    if (!classes) return false;
    if (classes == classname) return true;
    var whitespace = /\s+/;
    if (!whitespace.test(classes)) return false;
    if (typeof classes != 'string') return false;
    var c = classes.split(whitespace);
    for (var i = 0; i < c.length; i++) {
        if (c[i] == classname) return true;
    }
    return false;
}
function getElements(classname, classname2, classname3, classname4, tagname, root) {
    if (!root) root = document;
    else if (typeof root == "string") root = document.getElementById(root);
    if (!tagname) tagname = "*";
    var all = root.getElementsByTagName(tagname);
    if (!classname) return all;
    var elements = [];
    for (var i = 0; i < all.length; i++) {
        var element = all[i];
        if (isMember(element, classname) || isMember(element, classname2) || isMember(element, classname3) || isMember(element, classname4))
            elements.push(element);
    }
    return elements;

}

function bnumClick(index) {
    var msr1s = getElements("m1", "b2", "n", "l");
    if (msr1s.length <= index) return;
    var divId = 'bnumpanel' + String(index);
    var existingDiv = document.getElementById(divId);
    if (existingDiv != null) {
        existingDiv.remove();
        return;
    }
    var divParent = msr1s[index].className == "m1" ? msr1s[index].parentElement : msr1s[index];
    divParent.innerHTML = divParent.innerHTML + '<div class="bnumdiv" id="' + divId + '"><p>سلام</p></div>';
    
}

function coupletNumImage(bnum, color) {
    let canvas = document.createElement('canvas');
    canvas.width = 50;
    canvas.height = 28;
    let context = canvas.getContext('2d');
    context.font = "1.5em Vazir";
    context.fillStyle = color;
    context.textAlign = "center";
    context.textBaseline = "top";
    context.imageSmoothingEnabled = true;
    context.fillText(bnum, canvas.width / 2, 7);
    return canvas.toDataURL();
}

function bshfarsinum(englishnum) {
    var result = "";
    for (var i = 0; i < englishnum.length; i++) {
        result = result + String.fromCharCode(englishnum.charCodeAt(i) + 0x6C0);
    }
    return result;
}



function btshmr_internal() {
    var bnum = getElements("bnum");
    if (bnum.length != 0) {
        for (var i = 0; i < bnum.length; ++i) {
            bnum[i].remove();
        }
        document.getElementById("bnum-button").innerText = "شماره‌گذاری ابیات";
        return true;
    }
    document.getElementById("bnum-button").innerText = "حذف شماره‌ها";
    var msr1s = getElements("m1", "b2", "n", "l");
    if (msr1s.length == 0) return true;
    var j = 0;
    var k = 1;
    for (var i = 0; i < msr1s.length; ++i) {
        if (msr1s[i].className != "b2") {
            if (msr1s[i].className != "m1") {
                msr1s[i].innerHTML = '<div class="bnum normalbnum" onclick="bnumClick(' + String(i) + ');" id="bnum' + String(i + 1) + '"></div>' + msr1s[i].innerHTML;//no alt, so that when user copies text does not appear in the copied content
            }
            else {
                msr1s[i].parentElement.innerHTML = '<div class="bnum normalbnum" onclick="bnumClick(' + String(i) + ');" id="bnum' + String(i + 1) + '"></div>' + msr1s[i].parentElement.innerHTML;//no alt, so that when user copies text does not appear in the copied content
            }
            
            document.getElementById("bnum" + String(i + 1)).style.background = 'url(' + coupletNumImage(bshfarsinum(String(j + 1)), "red") + ')';
            j++;
        }
        else {
            msr1s[i].innerHTML = '<div class="bnum bandnum" onclick="bnumClick(' + String(i) + ');" id="bnum' + String(i + 1) + '"></div>' + msr1s[i].innerHTML;
            document.getElementById("bnum" + String(i + 1)).style.background = 'url(' + coupletNumImage("بند " + bshfarsinum(String(k)), "blue") + ')';
            k++;
            j = 0;
        }
    }
    return true;
}

function btshmr() {
    setTimeout(function () { btshmr_internal(); }, 1);
}






function hilightverse(vnum, clr, sc, forceScroll) {
    var root = document;
    if (typeof root == "string") root = document.getElementById(root);
    var all = root.getElementsByTagName("*");
    var n = -1;
    for (var i = 0; i < all.length; i++) {
        var element = all[i];
        if (isMember(element, "m1") || isMember(element, "m2") || isMember(element, "n") || isMember(element, "l")) {
            n++;
            if (n == vnum) {
                element.style.color = clr;
                if (element.lastChild.getElementsByTagName("BUTTON").length == 1) {
                    element.lastChild.removeChild(element.lastChild.getElementsByTagName("BUTTON")[0]);
                }
                if (sc == true) {
                    var btn = document.createElement("BUTTON");
                    var t = document.createTextNode(" || ");
                    btn.appendChild(t);
                    btn.style["display"] = "inline";
                    btn.style["width"] = "25px";
                    btn.onclick = function () {
                        if ($('#jquery_jplayer_1').data().jPlayer.status.paused) {
                            $('#jquery_jplayer_1').data().jPlayer.play();
                        }
                        else {
                            $('#jquery_jplayer_1').data().jPlayer.pause();
                        }
                    };
                    element.lastChild.appendChild(btn);

                    if (forceScroll)
                        if (!!element && element.scrollIntoView) {
                            element.scrollIntoView();
                        }
                }
                return true;
            }
        }
        else
            if (isMember(element, "b2")) {
                var ptags = element.getElementsByTagName("p");
                for (var p = 0; p < ptags.length; p++) {
                    if (ptags[p].parentElement.className == "bnum")
                        continue;
                    n++;
                    if (n == vnum) {
                        ptags[p].style.color = clr;
                        if (ptags[p].getElementsByTagName("BUTTON").length == 1) {
                            ptags[p].removeChild(ptags[p].getElementsByTagName("BUTTON")[0]);
                        }

                        if (sc == true) {
                            var btn = document.createElement("BUTTON");
                            var t = document.createTextNode(" || ");
                            btn.appendChild(t);
                            btn.style["display"] = "inline";
                            btn.style["width"] = "25px";
                            btn.onclick = function () {
                                if ($('#jquery_jplayer_1').data().jPlayer.status.paused) {
                                    $('#jquery_jplayer_1').data().jPlayer.play();
                                }
                                else {
                                    $('#jquery_jplayer_1').data().jPlayer.pause();
                                }
                            };
                            ptags[p].appendChild(btn);

                            if (forceScroll)
                                if (!!element && element.scrollIntoView) {
                                    element.scrollIntoView();
                                }
                        }
                        return true;
                    }
                }

            }
    }
    return false;
}

var audioxmlfiles = [];
function addpaudio(index, jplaylist, xmlfilename, poemtitle, auartist, oggurl, mp3url) {
    audioxmlfiles[index] = xmlfilename;
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


    var jlist = new jPlayerPlaylist({
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

