// From David Flanagan's "JavaScript: The Definitive Guide" 5th Ed,
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

function bnumClick(poemId, index) {
    var msr1s = getElements("m1", "b2", "n", "l");
    if (msr1s.length <= index) return;
    var divId = 'bnumpanel' + String(index);
    var existingDiv = document.getElementById(divId);
    if (existingDiv != null) {
        existingDiv.remove();
        return;
    }
    var divParent = msr1s[index].className == "m1" ? msr1s[index].parentElement : msr1s[index];
    var imgElementId = 'loadingimg-' + divId;
    divParent.innerHTML = divParent.innerHTML + '<div class="bnumdiv" id="' + divId + '"><img id="' + imgElementId + '" src="/image/loading.gif" alt="بارگذاری"/></div>';
    $.ajax({
        type: "GET",
        url: '?Handler=BNumPartial&poemId=' + String(poemId) + '&coupletIndex=' + String(index),
        success: function (data) {

            document.getElementById(imgElementId).style.display = "none";
            var divId = 'bnumpanel' + String(index);
            $(data).appendTo(document.getElementById(divId));
        },
    });

}


function coupletNumImage(bnum, color) {
    let canvas = document.createElement('canvas');
    canvas.width = 50;
    canvas.height = 28;
    let context = canvas.getContext('2d');
    context.font = "1.5em 'Vazirmatn'";
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



function btshmr_internal(poemId) {
    var bnum = getElements("bnum");
    if (bnum.length != 0) {
        lineNumbers = false;
        for (var i = 0; i < bnum.length; ++i) {
            bnum[i].remove();
        }
        var bnumdiv = getElements("bnumdiv");
        for (var i = 0; i < bnumdiv.length; ++i) {
            bnumdiv[i].remove();
        }
    }
    else {
        lineNumbers = true;
        var msr1s = getElements("m1", "b2", "n", "l");
        if (msr1s.length == 0) return true;
        var j = 0;
        var k = 1;
        for (var i = 0; i < msr1s.length; ++i) {
            if (msr1s[i].className != "b2") {
                if (msr1s[i].className != "m1") {
                    msr1s[i].innerHTML = '<div class="bnum normalbnum" onclick="bnumClick(' + String(poemId) + ',' + String(i) + ');" id="bnum' + String(i + 1) + '"></div>' + msr1s[i].innerHTML;//no alt, so that when user copies text does not appear in the copied content
                }
                else {
                    msr1s[i].parentElement.innerHTML = '<div class="bnum normalbnum" onclick="bnumClick(' + String(poemId) + ',' + String(i) + ');" id="bnum' + String(i + 1) + '"></div>' + msr1s[i].parentElement.innerHTML;//no alt, so that when user copies text does not appear in the copied content
                }

                document.getElementById("bnum" + String(i + 1)).style.background = 'url(' + coupletNumImage(bshfarsinum(String(j + 1)), "red") + ')';
                j++;
            }
            else {
                msr1s[i].innerHTML = '<div class="bnum bandnum" onclick="bnumClick(' + String(poemId) + ',' + String(i) + ');" id="bnum' + String(i + 1) + '"></div>' + msr1s[i].innerHTML;
                document.getElementById("bnum" + String(i + 1)).style.background = 'url(' + coupletNumImage("بند " + bshfarsinum(String(k)), "blue") + ')';
                k++;
                j = 0;
            }
        }
    }
    setCookie("lineNumbers", lineNumbers ? "true" : "false", 365);
    var btnLineNumbers = document.getElementById("bnum-button");
    if (lineNumbers) {
        btnLineNumbers.classList.remove("color-disabled");
        btnLineNumbers.classList.add("color-white");
    }
    else {
        btnLineNumbers.classList.remove("color-white");;
        btnLineNumbers.classList.add("color-disabled")
    }

    return true;
}

function btshmr(poemId) {
    setTimeout(function () { btshmr_internal(poemId); }, 1);
}

function switchPlayerScrollLock() {
    playerScrollLock = !playerScrollLock;
    setCookie("playerScrollLock", playerScrollLock ? "true" : "false", 365);
    if (playerScrollLock) {

        $('#scroll-lock').text('🔒');
    }
    else {
        $('#scroll-lock').text('🔓');
    }

    if (playerScrollLock) {
        const lockButtons = document.querySelectorAll('.recitation-scrollunlock');
        lockButtons.forEach(function (lockButton) {
            lockButton.classList.toggle('recitation-scrollunlock');
            lockButton.classList.toggle('recitation-scrolllock');
        });
        alert('قفل متن روی خوانش فعال شد.');
    }
    else {
        const lockButtons = document.querySelectorAll('.recitation-scrolllock');
        lockButtons.forEach(function (lockButtons) {
            lockButtons.classList.toggle('recitation-scrollunlock');
            lockButtons.classList.toggle('recitation-scrolllock');
        });
        alert('قفل متن روی خوانش غیرفعال شد.');
    }
}

function scrollToTargetAdjusted(element) {
    var stickyNavbar = document.getElementsByClassName("sticky");
    var headerOffset = stickyNavbar.length == 0 ? 0 : document.getElementById('main-navbar').clientHeight;
    var elementPosition = element.getBoundingClientRect().top;
    var offsetPosition = elementPosition + window.scrollY - headerOffset;

    window.scrollTo({
        top: offsetPosition,
        behavior: "smooth"
    });
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
                if ($('#InlinePauseButton') != null) {
                    $('#InlinePauseButton').remove();
                }
                if ($('#InlineLockButton') != null) {
                    $('#InlineLockButton').remove();
                }
                if (sc == true) {
                    var btn = document.createElement("a");
                    btn.id = "InlinePauseButton";
                    btn.setAttribute('role', 'button');
                    btn.className = 'inlineanchor';
                    btn.onclick = function () {
                        if (currentAudio.paused) {
                            currentAudio.play();
                            $('#InlinePauseButtonImage').text('pause_circle_filled');
                        }
                        else {
                            currentAudio.pause();
                            $('#InlinePauseButtonImage').text('play_circle_filled');
                        }
                    };
                    element.lastChild.appendChild(btn);

                    var btnIcon = document.createElement("i");
                    btnIcon.className = 'inlinebutton';
                    btnIcon.innerText = 'pause_circle_filled';
                    btnIcon.id = "InlinePauseButtonImage"
                    btn.appendChild(btnIcon);

                    var btnLock = document.createElement("a");
                    btnLock.id = "InlineLockButton";
                    btnLock.setAttribute('role', 'button');
                    btnLock.className = 'inlineanchor';
                    btnLock.onclick = switchPlayerScrollLock;
                    element.lastChild.appendChild(btnLock);

                    var btnLockIcon = document.createElement("i");
                    btnLockIcon.className = 'inlinebutton';
                    btnLockIcon.innerText = 'lock';
                    btnLockIcon.id = "InlineLockButtonImage"
                    btnLock.appendChild(btnLockIcon);

                    if (forceScroll)
                        if (!!element && element.scrollIntoView) {
                            scrollToTargetAdjusted(element);
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
                        if ($('#InlinePauseButton') != null) {
                            $('#InlinePauseButton').remove();
                        }
                        if ($('#InlineLockButton') != null) {
                            $('#InlineLockButton').remove();
                        }

                        if (sc == true) {
                            var btn = document.createElement("a");
                            btn.id = "InlinePauseButton";
                            btn.setAttribute('role', 'button');
                            btn.className = 'inlineanchor';
                            btn.onclick = function () {
                                if (currentAudio.paused) {
                                    currentAudio.play();
                                    $('#InlinePauseButtonImage').text('pause_circle_filled');
                                }
                                else {
                                    currentAudio.pause();
                                    $('#InlinePauseButtonImage').text('play_circle_filled');
                                }
                            };
                            ptags[p].appendChild(btn);
                            var btnIcon = document.createElement("i");
                            btnIcon.className = 'inlinebutton';
                            btnIcon.innerText = 'pause_circle_filled';
                            btnIcon.id = "InlinePauseButtonImage"
                            btn.appendChild(btnIcon);

                            var btnLock = document.createElement("a");
                            btnLock.id = "InlineLockButton";
                            btnLock.setAttribute('role', 'button');
                            btnLock.className = 'inlineanchor';
                            btnLock.onclick = function () {
                                playerScrollLock = !playerScrollLock;
                                setCookie("playerScrollLock", playerScrollLock ? "true" : "false", 365);
                                if (playerScrollLock) {

                                    $('#scroll-lock').text('🔒');
                                }
                                else {
                                    $('#scroll-lock').text('🔓');
                                }
                            };
                            ptags[p].appendChild(btnLock);
                            var btnLockIcon = document.createElement("i");
                            btnLockIcon.className = 'inlinebutton';
                            btnLockIcon.innerText = 'lock';
                            btnLockIcon.id = "InlineLockButtonImage"
                            btnLock.appendChild(btnLockIcon);

                            if (forceScroll)
                                if (!!element && element.scrollIntoView) {
                                    scrollToTargetAdjusted(element);
                                }
                        }
                        return true;
                    }
                }

            }
    }
    return false;
}

function fillnarrations(coupletIndex) {
    if (typeof (narrators) == "undefined") {
        var blockid = '#play-block-' + coupletIndex;
        $(blockid).hide();
        return;
    }
    if (narrators.length == 0) {
        var blockid = '#play-block-' + coupletIndex;
        $(blockid).hide();
        return;
    }

    var comboId = '#narrators-' + coupletIndex;
    for (var i = 0; i < narrators.length; i++) {
        $(comboId).append(new Option(narrators[i].replace(/<\/?[^>]+(>|$)/g, "").replace('به خوانش ', '').replace('می‌خواهید شما بخوانید؟ اینجا را ببینید.', '').replace('(دریافت)', ''), i));
    }
}

function getVerseIndexFromCoupleIndex(coupletIndex) {
    var tagname = "*";
    var all = document.getElementsByTagName(tagname);
    var msr1s = [];
    for (var i = 0; i < all.length; i++) {
        var element = all[i];
        if (isMember(element, "m1") || isMember(element, "m2") || isMember(element, "b2") || isMember(element, "n") || isMember(element, "l"))
            msr1s.push(element);
    }
    if ((msr1s.length - 1) < coupletIndex) return -1;
    var cIndex = -1;
    var vIndex = -1;
    var bandNums = 0;
    for (var i = 0; i < msr1s.length; ++i) {
        if (msr1s[i].className != "m2") {
            cIndex++;
        }

        if (cIndex == coupletIndex) {
            vIndex = (i + bandNums);
            break;
        }

        if (msr1s[i].className == "b2") {
            if (msr1s[i].getElementsByTagName("p").length > 1)
                bandNums++;
        }

    }
    return vIndex;
}


function playCouplet(coupletIndex) {

    var vIndex = getVerseIndexFromCoupleIndex(coupletIndex);
    var comboId = '#narrators-' + coupletIndex;
    var recitationIndex = parseInt($(comboId).find(":selected").val());
    var recitationOrder = recitationIndex + 1;



    if (audioxmlfiles.length > 0) {
        $.ajax({
            type: "GET",
            url: audioxmlfiles[recitationIndex],
            dataType: "xml",
            success: function (xml) {
                var nOneSecondBugFix = 2000;
                $(xml).find('OneSecondBugFix').each(function () {
                    nOneSecondBugFix = parseInt($(xml).find('OneSecondBugFix').text());
                });

                var foundCouplet = false;

                $(xml).find('SyncInfo').each(function () {
                    var v = parseInt($(this).find('VerseOrder').text())
                    if (v == vIndex) {
                        var verseStart = parseInt($(this).find('AudioMiliseconds').text()) / nOneSecondBugFix;
                        var audioControl = document.getElementById('audio-' + String(recitaionIds[recitationIndex]));
                        audioControl.play();
                        var buttonList = '#listen-' + coupletIndex;
                        $(buttonList).text('در حال دریافت خوانش ...');
                        setTimeout(function () {
                            audioControl.currentTime = verseStart;
                            $(buttonList).text('در حال خواندن');
                        }, 100);
                        foundCouplet = true;
                        return false;
                    }
                });

                if (!foundCouplet) {
                    alert('در این خوانش این خط خوانده نشده است.');
                }
            }
        });
    }
}

function editCouplet(poemId, coupletIndex, blockId) {

    var vIndex = getVerseIndexFromCoupleIndex(coupletIndex);
    location.href = '/User/Editor?id=' + poemId + '#' + blockId + '-' + String(vIndex + 1);
}

function switchBookmark(poemId, coupletIndex, divSuffix) {
    $.ajax({
        type: "GET",
        url: '?Handler=PoemBookmarks&poemId=' + String(poemId),
        error: function (err) {
            alert('PoemBookmarks: ' + err.toString());
        },
        success: function (bookmarks) {
            for (var i = 0; i < bookmarks.length; i++) {
                var bookmark = bookmarks[i];
                if (bookmark.coupletIndex == coupletIndex) {
                    if (bookmark.privateNote != null && bookmark.privateNote != '') {
                        if (!confirm('با حذف این نشان یادداشت آن نیز حذف خواهد شد. از این کار اطمینان دارید؟')) {
                            return;
                        }
                    }
                    break;
                }
            }
            switchBookmarkInternal(poemId, coupletIndex, divSuffix);
        },
    });
}

function switchBookmarkInternal(poemId, coupletIndex, divSuffix) {
    var iconElementId = coupletIndex < 0 ? 'bookmark-icon-comment-' + String(-coupletIndex) + divSuffix : 'bookmark-icon-' + String(coupletIndex);
    var secondIconElementId = divSuffix != '' && coupletIndex < 0 ? 'bookmark-icon-comment-' + String(-coupletIndex) : null;
    if (document.getElementById(iconElementId) != null) {
        document.getElementById(iconElementId).innerHTML = 'star_half';
    }
    if (document.getElementById(secondIconElementId) != null) {
        document.getElementById(secondIconElementId).innerHTML = 'star_half';
    }
    if (coupletIndex == 0) {
        document.getElementById('bookmark-icon').innerHTML = 'star_half';
    }
    var url = '/?handler=SwitchBookmark';

    $.ajax({
        type: "POST",
        url: url,
        data: {
            poemId: poemId,
            coupletIndex: coupletIndex
        },
        error: function (err) {
            alert('SwitchBookmark: ' + err.toString())
        },
        success: function (bookmarkId) {
            var isBookmarked = bookmarkId != '0';
            if (isBookmarked == true) {
                if (document.getElementById(iconElementId) != null) {
                    document.getElementById(iconElementId).innerHTML = 'star';
                }
                if (document.getElementById(secondIconElementId) != null) {
                    document.getElementById(secondIconElementId).innerHTML = 'star';
                }

                if (coupletIndex == 0) {
                    document.getElementById('bookmark').innerHTML = '<i class="noindent-info-button color-yellow" id="bookmark-icon">star</i>';
                }

                if (coupletIndex >= 0) {
                    tinymce.get('editNoteText').setContent('');
                    $("#editbookmarkId").val(bookmarkId);
                    document.getElementById('bookmark-note-dialog').style.display = 'block';
                }
            }
            else {
                if (document.getElementById(iconElementId) != null) {
                    document.getElementById(iconElementId).innerHTML = 'star_border';
                }

                if (document.getElementById(secondIconElementId) != null) {
                    document.getElementById(secondIconElementId).innerHTML = 'star_border';
                }

                if (coupletIndex == 0) {
                    document.getElementById('bookmark').innerHTML = '<i class="noindent-info-button color-white" id="bookmark-icon">star_border</i>';
                }
            }
        },

    });
}

function checkIfBookmarked(poemId) {
    if (document.getElementById('bookmark-icon') == null) return;
    setTimeout(function () {
        $.ajax({
            type: "GET",
            url: '?Handler=PoemBookmarks&poemId=' + String(poemId),
            success: function (bookmarks) {
                var isBookmarked = false;
                for (var i = 0; i < bookmarks.length; i++) {
                    var bookmark = bookmarks[i];
                    if (bookmark.coupletIndex == 0) {
                        isBookmarked = true;
                    }
                    else if (bookmark.coupletIndex < 0) {
                        var commentBookmark = document.getElementById('bookmark-comment-' + (-bookmark.coupletIndex).toString());
                        if (commentBookmark != null) {
                            commentBookmark.innerHTML = '<i class="pageicons" id="bookmark-icon-comment-' + (-bookmark.coupletIndex).toString() + '">star</i>';
                        }
                    }
                }
                if (isBookmarked) {
                    document.getElementById('bookmark').innerHTML = '<i class="noindent-info-button color-yellow" id="bookmark-icon">star</i>';
                }
                else {
                    document.getElementById('bookmark').innerHTML = '<i class="noindent-info-button color-white" id="bookmark-icon">star_border</i>';
                }
            },
        });
    }, 1);
}

function savePrivateNote() {
    $("#editnoteform").unbind('submit').bind('submit', function (e) {

        e.preventDefault(); // avoid to execute the actual submit of the form.

        var url = '?handler=BookmarkNote';

        var bookmarkId = $("#editbookmarkId").val();

        $.ajax({
            type: "PUT",
            url: url,
            data: {
                id: bookmarkId,
                note: $("textarea#editNoteText").val()
            },
            success: function () {
                document.getElementById('bookmark-note-dialog').style.display = 'none';
            },

        });
    });
}

function checkWebShareSupport() {
    if (navigator.share === undefined) {
        document.getElementById('share_span').style.display = 'none';
    }
}

function getPoemText() {
    var all = $('#garticle').children();
    var text = '';
    for (var i = 0; i < all.length; i++) {
        var element = all[i];
        if (element.className == 'b' || element.className == 'b2' || element.className == 'n' || element.className == 'l') {
            var elementSubs = $('#' + element.id).children();
            for (var j = 0; j < elementSubs.length; j++) {
                var elementSub = elementSubs[j];
                if (elementSub.className == 'm1' || elementSub.className == 'm2' || elementSub.nodeName == 'P') {
                    if (text != '')
                        text += '\n';
                    text += elementSub.innerText;
                }
                if (elementSub.className == 'm2' || elementSub.className == 'b2') {
                    if (text != '')
                        text += '\n';
                }
            }
        }
    }
    return text;
}

function getSummeriesText() {
    var all = $('#summaries').children();
    var text = '';
    for (var i = 0; i < all.length; i++) {
        var element = all[i];
        if (element.className == 'coupletsummary') {
            var elementSubs = $('#' + element.id).children();
            for (var j = 0; j < elementSubs.length; j++) {
                var elementSub = elementSubs[j];
                if (elementSub.nodeName == 'BLOCKQUOTE') {
                    var quoteSubs = $('#' + elementSub.id).children();
                    for (var k = 0; k < quoteSubs.length; k++) {
                        if (quoteSubs[k].nodeName == 'P') {
                            if (text != '')
                                text += '\n';
                            text += quoteSubs[k].innerText.replace('#', '').trim();
                        }
                    }
                }
                else
                    if (elementSub.className == 'notice') {
                        var noticeSubs = $('#' + elementSub.id).children();
                        for (var k = 0; k < noticeSubs.length; k++) {
                            if (noticeSubs[k].nodeName == 'P') {
                                if (text != '') {
                                    text += '\n\n';
                                }
                                text += noticeSubs[k].innerText;
                                text += '\n\n\n';
                            }
                        }
                }
            }
        }
    }
    return text;
}

async function webSharePoem() {
    var text = getPoemText();
    var title = document.title;
    var url = window.location.href;
    try {
        await navigator.share({ title, text, url });
    } catch (error) {
        alert('Error sharing: ' + error);
    }
}

function copyPoemText() {
    var text = getPoemText();
    navigator.clipboard.writeText(text);
    alert('متن در حافظه رونوشت شد.');
}

function copyPoemLink() {
    var url = window.location.href;
    if (url.indexOf('#') != -1) {
        url = url.substring(0, url.indexOf('#'));
    }
    navigator.clipboard.writeText(url);
    alert('نشانی در حافظه رونوشت شد.');
}

function copyCoupletUrl(coupletIndex) {
    var url = window.location.href;
    if (url.indexOf('#') != -1) {
        url = url.substring(0, url.indexOf('#'));
    }
    url += ('#bn' + String(coupletIndex + 1));
    navigator.clipboard.writeText(url);
    alert('نشانی در حافظه رونوشت شد.');
}

function getCoupletText(coupletIndex) {
    var all = $('#bn' + String(coupletIndex + 1)).children();
    var text = '';
    for (var i = 0; i < all.length; i++) {
        var element = all[i];
        if (element.className == 'm1' || element.className == 'm2' || element.nodeName == 'P') {
            text += element.innerText;
            text += '\n';
        }
    }
    return text;
}

async function webShareCouplet(coupletIndex) {
    var text = getCoupletText(coupletIndex);
    var title = document.title;
    var url = window.location.href + '#bn' + String(coupletIndex + 1);
    try {
        await navigator.share({ title, text, url });
    } catch (error) {
        alert('از همرسانی روی مرورگر جاری شما پشتیبانی نمی‌شود.')

    }
}

function copyCoupletText(coupletIndex) {
    var text = getCoupletText(coupletIndex);
    navigator.clipboard.writeText(text);
    alert('متن در حافظه رونوشت شد.');
}

function copySummeriesText() {
    var text = getSummeriesText();
    navigator.clipboard.writeText(text);
    alert('متن در حافظه رونوشت شد.');
}


function wpopen(macagna) {
    window.open(macagna, '_blank', 'width=600,height=700,scrollbars=yes,status=yes');
}

function postComment(coupletIndex, buttonSelector) {
    var commentformSelector = '#commentform';
    if (coupletIndex != '') {
        commentformSelector += ('-' + coupletIndex);
    }
    $(commentformSelector).unbind('submit').bind('submit', function (e) {

        e.preventDefault(); // avoid to execute the actual submit of the form.

        $(buttonSelector).text('در حال درج حاشیه ...');
        $(buttonSelector).prop("disabled", true);

        $('#comment-error').remove();

        var parent1 = $('#comments-block');
        var parent2 = null;
        if (coupletIndex != '') {
            parent2 = $('#comments-block-' + coupletIndex);
        }

        var form = $(this);
        var url = form.attr('action');

        $.ajax({
            type: "POST",
            url: url,
            data: form.serialize(), // serializes the form's elements.
            error: function () {
                $(buttonSelector).text('درج حاشیه');
                $(buttonSelector).prop("disabled", false);
            },
            success: function (data) {
                $(data).appendTo(parent1);
                if (parent2 != null)
                    $(data).appendTo(parent2);
                $(buttonSelector).text('درج حاشیه');
                $(buttonSelector).prop("disabled", false);
                form[0].reset();
            },
        });

    });
}


function replyComment(commentId, loggedIn, divSuffix) {
    if (!loggedIn) {
        alert('برای پاسخگویی لازم است با نام کاربری خود وارد گنجور شوید.');
        return;
    }
    $("#refComment").html($('#comment-text-' + commentId + divSuffix).html());
    $("#refCommentId").val(commentId);
    $("#refCommentDivSuffix").val(divSuffix);
    document.getElementById('id03').style.display = 'block';
}

function postReplyComment() {
    parent = $('#comment-' + $("#refCommentId").val() + $("#refCommentDivSuffix").val());
    $("#replycommentform").unbind('submit').bind('submit', function (e) {

        e.preventDefault(); // avoid to execute the actual submit of the form.

        $('#replycomment').text('در حال درج پاسخ به حاشیه ...');
        $('#replycomment').prop("disabled", true);

        $('#comment-error').remove();

        var form = $(this);
        var url = form.attr('action');

        $.ajax({
            type: "POST",
            url: url,
            data: form.serialize(), // serializes the form's elements.
            error: function () {
                $('#replycomment').text('درج پاسخ به حاشیه');
                $('#replycomment').prop("disabled", false);
            },
            success: function (data) {
                document.getElementById('id03').style.display = 'none';
                $(data).appendTo(parent);
                $('#replycomment').prop("disabled", false);
                $("#replycommentform")[0].reset();
            },
        });

    });
}

function deleteMyComment(commentId, coupletIndex) {
    if (!confirm('آیا از حذف این حاشیه اطمینان دارید؟'))
        return;

    var url = '?handler=MyComment';

    $.ajax({
        type: "DELETE",
        url: url,
        data: {
            id: commentId
        },
        success: function () {
            var commentBlockId = '#comment-' + commentId;
            if ($(commentBlockId) != null) {
                $(commentBlockId).remove();
            }
            if (coupletIndex != '-1') {
                var bnumCommentBlockId = '#comment-' + commentId + "-" + coupletIndex;
                if ($(bnumCommentBlockId) != null) {
                    $(bnumCommentBlockId).remove();
                }
            }
        },
    });

}
function deleteMistake(mistakeId) {
    if (!confirm('آیا از حذف این اشکال اطمینان دارید؟'))
        return;

    var url = '?handler=Mistake';

    $.ajax({
        type: "DELETE",
        url: url,
        data: {
            id: mistakeId
        },
        success: function () {
            var commentBlockId = '#mistake-' + mistakeId;
            if ($(commentBlockId) != null) {
                $(commentBlockId).remove();
            }
        },
    });
}

function editMistakeReason(mistakeId, reasonText) {
    var edited = prompt('ویرایش', reasonText);

    if (edited == null) return;
   

    var url = '?handler=Mistake';

    $.ajax({
        type: "PUT",
        url: url,
        data: {
            id: mistakeId,
            reasonText: edited,
        },
        success: function () {
            alert('انجام شد.');
        },
    });
}

function editMyComment(commentId, coupletIndex) {
    var commentTextBlockId = '#comment-text-' + commentId;
    tinymce.get('editCommentText').setContent($(commentTextBlockId).html());
    $("#editCommentId").val(commentId);
    $("#editCommentCoupletIndex").val(coupletIndex);
    document.getElementById('id02').style.display = 'block';
}




function editComment() {
    $("#editcommentform").unbind('submit').bind('submit', function (e) {

        e.preventDefault(); // avoid to execute the actual submit of the form.

        $('#editcomment').text('در حال ویرایش حاشیه ...');
        $('#editcomment').prop("disabled", true);

        var url = '?handler=MyComment';

        var commentId = $("#editCommentId").val();
        var coupletIndex = $("#editCommentCoupletIndex").val();


        $.ajax({
            type: "PUT",
            url: url,
            data: {
                id: commentId,
                comment: $("textarea#editCommentText").val()
            },
            error: function () {
                $('#editcomment').text('ویرایش حاشیه');
                $('#editcomment').prop("disabled", false);
            },
            success: function () {
                document.getElementById('id02').style.display = 'none';

                var commentTextBlockId = '#comment-text-' + commentId;

                $(commentTextBlockId).html($("textarea#editCommentText").val());

                if (coupletIndex != '-1') {
                    var bnumCommentBlockId = '#comment-text-' + commentId + "-" + coupletIndex;
                    if ($(bnumCommentBlockId) != null) {
                        $(bnumCommentBlockId).html($("textarea#editCommentText").val());
                    }
                }

                $('#editcomment').text('ویرایش حاشیه');
                $('#editcomment').prop("disabled", false);
            },

        });
    });
}

function copyCommentUrl(commentId) {
    var url = window.location.href;
    if (url.indexOf('#') != -1) {
        url = url.substring(0, url.indexOf('#'));
    }
    if (url.indexOf('?') != -1) {
        url += '&';
    }
    else {
        url += '?'
    }
    url += 'tab=discussions';
    url += ('#comment-' + String(commentId));
    navigator.clipboard.writeText(url);
    alert('نشانی در حافظه رونوشت شد.');
}

function onSelectedPoetChanged() {
    if (document.getElementById('cat') != null) {
        var id = document.getElementById('author').value;
        if (id == 0) return;
        $.ajax({
            type: "GET",
            url: '?handler=PoetInformation',
            data: {
                id: id
            },
            success: function (poet) {
                if (poet == null)
                    return;
                var select = document.getElementById("cat");
                var length = select.options.length;
                for (var i = length - 1; i >= 0; i--) {
                    select.options[i] = null;
                }
                option = document.createElement('option');
                option.setAttribute('value', 0);
                option.appendChild(document.createTextNode('در همهٔ بخشها'));
                select.appendChild(option);
                for (var i = 0; i < poet.cat.children.length; i++) {
                    option = document.createElement('option');
                    option.setAttribute('value', poet.cat.children[i].id);
                    option.appendChild(document.createTextNode(poet.cat.children[i].title));
                    select.appendChild(option);
                }
            },
        });
    }
}


function loadMoreRelatedPoems(poemId, skip, rhythm, rhymeLetters, poemFullUrl, sectionIndex) {
    var loadButton = document.getElementById('load-more-button-' + String(sectionIndex));
    if (loadButton != null) {
        loadButton.remove();
    }
    var divParent = document.getElementById('load-more-related-' + String(sectionIndex));
    var imgeId = 'load-more-related-loadingimg-' + String(sectionIndex);
    divParent.innerHTML = divParent.innerHTML + '<img id="' + imgeId + '" src="/image/loading.gif" alt="بارگذاری  "/>';

    $.ajax({
        type: "GET",
        url: '?Handler=SimilarPoemsPartial&poemId=' + String(poemId) + '&skip=' + String(skip) + '&prosodyMetre=' + rhythm + '&rhymeLetters=' + rhymeLetters + '&poemFullUrl=' + poemFullUrl + '&sectionId=' + String(sectionIndex),
        success: function (data) {

            var imgElement = document.getElementById(imgeId);
            imgElement.remove();

            $(data).appendTo(document.getElementById('more-related-placeholder-' + String(sectionIndex)));
        },
    });
}

function loadMoreRelatedFromPoet(poetId, rhythm, rhymeLetters, skipPoemFullUrl1, skipPoemFullUrl2, sectionIndex) {
    var buttonId = 'load-more-button-' + String(poetId) + '-' + String(sectionIndex);
    var loadButton = document.getElementById(buttonId);
    if (loadButton != null) {
        loadButton.remove();
    }
    var divId = 'more-related-placeholder-' + String(poetId) + '-' + String(sectionIndex);
    var divParent = document.getElementById(divId);
    var imgeId = 'load-more-related-loadingimg-' + String(poetId) + '-' + String(sectionIndex);
    divParent.innerHTML = divParent.innerHTML + '<img id="' + imgeId + '" src="/image/loading.gif" alt="بارگذاری  "/>';

    $.ajax({
        type: "GET",
        url: '?Handler=SimilarPoemsFromPoetPartial&poetId=' + String(poetId) + '&prosodyMetre=' + rhythm + '&rhymeLetters=' + rhymeLetters + '&skipPoemFullUrl1=' + skipPoemFullUrl1 + '&skipPoemFullUrl2=' + skipPoemFullUrl2,
        success: function (data) {

            var imgElement = document.getElementById(imgeId);
            imgElement.remove();

            $(data).appendTo(document.getElementById(divId));
        },
    });
}

function showAllRecitations() {
    const hiddenRecitations = document.querySelectorAll('.hidden-recitation');
    hiddenRecitations.forEach(function (recitation) {
        recitation.className = 'audio-player';
    });

    const showRecitationsButtons = document.querySelectorAll('.load-all-recitations');
    showRecitationsButtons.forEach(function (btn) {
        btn.style.display = 'none';
    });

}

function switch_section(sectionId, buttonId) {
    if (document.getElementById(sectionId).style.display == 'none') {
        document.getElementById(sectionId).style.display = 'block';
    }
    else {
        document.getElementById(sectionId).style.display = 'none';
    }
    document.getElementById(buttonId).classList.toggle('expand_circle_down');
    document.getElementById(buttonId).classList.toggle('collapse_circle_down');
}

function switchRecitationVote(recitationId) {
    $.ajax({
        type: "POST",
        url: '?Handler=SwitchRecitationUpVote',
        data: {
            id: recitationId
        },
        success: function (upVote) {
            if (upVote) {
                document.getElementById('recitaion-' + String(recitationId)).classList.remove('recitation-novote');
                document.getElementById('recitaion-' + String(recitationId)).classList.add('recitation-vote');
            }
            else {
                document.getElementById('recitaion-' + String(recitationId)).classList.remove('recitation-vote');
                document.getElementById('recitaion-' + String(recitationId)).classList.add('recitation-novote');
            }

        },
        error: function (e) {
            if (e.responseText == null)
                alert('SwitchRecitationUpVote: ' + e.toString());
            else
                alert('SwitchRecitationUpVote: ' +e.responseText);
        },
    });
}

function MarkUserUpvotedRecitations(poemId) {
    setTimeout(function () {
        $.ajax({
            type: "GET",
            url: '?Handler=UserUpvotedRecitations&poemId=' + String(poemId),
            success: function (result) {
                for (var i = 0; i < result.length; i++) {
                    document.getElementById('recitaion-' + String(result[i])).classList.add('recitation-vote');
                }
            },
        });
    }, 1);
}

function persianizeNumerals(value) {
    return value.replace(/0/gi, "۰").replace(/1/gi, "۱").replace(/2/gi, "۲")
        .replace(/3/gi, "۳").replace(/4/gi, "۴").replace(/5/gi, "۵")
        .replace(/6/gi, "۶").replace(/7/gi, "۷").replace(/8/gi, "۸")
        .replace(/9/gi, "۹");
}

function onInlineSearch(value, resultBlockId, itemsClass) {
    const foundPoetsNode = document.getElementById(resultBlockId);
    foundPoetsNode.innerHTML = '';
    if (value.length > 0) {
        let replaced = persianizeNumerals(value);
        var poets = document.getElementsByClassName(itemsClass);
        var foundOnes = [];
        for (var i = 0; i < poets.length; i++) {
            var dataValue = poets[i].getAttribute("data-value");
            if (dataValue != null) {
                if (dataValue.indexOf(replaced) != -1) {
                    foundOnes.push(poets[i]);
                }
            }
        }

        for (var i = 0; i < foundOnes.length; i++) {
            var clonedPoet = foundOnes[i].cloneNode(true);
            foundPoetsNode.appendChild(clonedPoet);
        }
    }
}

function AddToMyHistory(poemId) {
    setTimeout(function () {
        $.ajax({
            type: "POST",
            url: '?Handler=AddToMyHistory&poemId=' + String(poemId),
        });
    }, 1);
}

function CheckIfHasNotificationsForHomePage() {
    setTimeout(function () {
        $.ajax({
            type: "GET",
            url: '?Handler=CheckIfHasNotifications',
            success: function (result) {
                if (result != '') {
                    document.getElementById('notification-badge').classList.toggle('display-none');
                    document.getElementById('notification-badge').classList.toggle('visible-notification-badge');
                }
            },
        });
    }, 1);
}

function doSearchInRhythmsCombo(selectSearchId, rhythmnewId) {
    var value = document.getElementById(selectSearchId).value;
    var options = document.getElementById(rhythmnewId).options
    for (var i = 0; i < options.length; i++) {
        if (options[i].text.indexOf(value) != -1) {
            document.getElementById(rhythmnewId).value = options[i].value;
            break;
        }
    }
}

function resetRhythm(rhythmnewId) {
    document.getElementById(rhythmnewId).value = 'null';
}

function setCookie(cname, cvalue, exdays) {
    const d = new Date();
    d.setTime(d.getTime() + (exdays * 24 * 60 * 60 * 1000));
    let expires = "expires=" + d.toUTCString();
    document.cookie = cname + "=" + cvalue + ";" + expires + ";path=/";
}

function getCookie(cname) {
    let name = cname + "=";
    let decodedCookie = decodeURIComponent(document.cookie);
    let ca = decodedCookie.split(';');
    for (let i = 0; i < ca.length; i++) {
        let c = ca[i];
        while (c.charAt(0) == ' ') {
            c = c.substring(1);
        }
        if (c.indexOf(name) == 0) {
            return c.substring(name.length, c.length);
        }
    }
    return "";
}

function w3_open() {
    if (document.getElementById("mySidebar").style.display == "none") {
        document.getElementById("mySidebar").style.display = "block";
    }
    else {
        document.getElementById("mySidebar").style.display = "none";
    }
    
}

function w3_close() {
    document.getElementById("mySidebar").style.display = "none";
}

function w3_close_showHelp() {
    document.getElementById("mySidebar").style.display = "none";
    document.getElementById('navbarhelp').style.display = 'block'
}

function viewLocation(lt, lg) {
    link = document.createElement("a")
    link.href = 'https://maps.google.com/?q=' + lt + ',' + lg + '&ll=' + lt + ',' + lg + '&z=3';
    link.target = "_blank"
    link.click()
}

function loadCatRecitations(catId) {

    var loadButton = document.getElementById("load-cat-recitations");
    if (loadButton == null) {

        return;
    }
    loadButton.remove();

    var divParent = document.getElementById('recitations-section');
    var imgElementId = 'loadingimg';
    divParent.innerHTML = divParent.innerHTML + '<div class="bnumdiv" id="remove-this"><img id="' + imgElementId + '" src="/image/loading.gif" alt="بارگذاری"/></div>';
    $.ajax({
        type: "GET",
        url: '?Handler=CategoryRecitations&catId=' + String(catId),
        success: function (data) {
            document.getElementById("remove-this").remove();
            $(data).appendTo(divParent);
        },
    });

}

// https://stackoverflow.com/questions/56300132/how-to-override-css-prefers-color-scheme-setting
// Return the system level color scheme, but if something's in local storage, return that
// Unless the system scheme matches the the stored scheme, in which case... remove from local storage
function getPreferredColorScheme() {
    let systemScheme = 'light';
    if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
        systemScheme = 'dark';
    }
    let chosenScheme = systemScheme;

    if (localStorage.getItem("scheme")) {
        chosenScheme = localStorage.getItem("scheme");
    }

    if (systemScheme === chosenScheme) {
        localStorage.removeItem("scheme");
    }

    return chosenScheme;
}

// Write chosen color scheme to local storage
// Unless the system scheme matches the the stored scheme, in which case... remove from local storage
function savePreferredColorScheme(scheme) {
    let systemScheme = 'light';

    if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
        systemScheme = 'dark';
    }

    if (systemScheme === scheme) {
        localStorage.removeItem("scheme");
    }
    else {
        localStorage.setItem("scheme", scheme);
    }

}

// Get the current scheme, and apply the opposite
function toggleColorScheme() {
    let newScheme = "light";
    let scheme = getPreferredColorScheme();
    if (scheme === "light") {
        newScheme = "dark";
    }

    applyPreferredColorScheme(newScheme);
    savePreferredColorScheme(newScheme);


}

// Apply the chosen color scheme by traversing stylesheet rules, and applying a medium.
function applyPreferredColorScheme(scheme) {
    for (var s = 0; s < document.styleSheets.length; s++) {
        var sheet = document.styleSheets[s];
        var rules;
        try {
            rules = sheet.cssRules;
        } catch {
            continue;
        }
        for (var i = 0; i < rules.length; i++) {
            var rule = rules[i];
            if (rule && rule.media && rule.media.mediaText.includes("prefers-color-scheme")) {

                switch (scheme) {
                    case "light":
                        rule.media.appendMedium("original-prefers-color-scheme");
                        if (rule.media.mediaText.includes("light")) rule.media.deleteMedium("(prefers-color-scheme: light)");
                        if (rule.media.mediaText.includes("dark")) rule.media.deleteMedium("(prefers-color-scheme: dark)");
                        break;
                    case "dark":
                        rule.media.appendMedium("(prefers-color-scheme: light)");
                        rule.media.appendMedium("(prefers-color-scheme: dark)");
                        if (rule.media.mediaText.includes("original")) rule.media.deleteMedium("original-prefers-color-scheme");
                        break;
                    default:
                        rule.media.appendMedium("(prefers-color-scheme: dark)");
                        if (rule.media.mediaText.includes("light")) rule.media.deleteMedium("(prefers-color-scheme: light)");
                        if (rule.media.mediaText.includes("original")) rule.media.deleteMedium("original-prefers-color-scheme");
                        break;
                }
            }
        }


    }

    // Change the toggle button to be the opposite of the current scheme
    if (scheme === "dark") {
        if (document.getElementById("icon-sun") != null) {
            document.getElementById("icon-sun").style.display = 'inline';
            document.getElementById("icon-moon").style.display = 'none';
        }

    } else {
        if (document.getElementById("icon-sun") != null) {
            document.getElementById("icon-moon").style.display = 'inline';
            document.getElementById("icon-sun").style.display = 'none';
        }
    }
}

function countAndSortCharacters(inputString) {
    // Initialize an empty map to store character counts
    const charCountMap = new Map();

    // Iterate through each character in the input string
    for (const char of inputString) {
        if (char == ' ') continue;
        // Check if the character is already in the map
        if (charCountMap.has(char)) {
            // If yes, increment the count
            charCountMap.set(char, charCountMap.get(char) + 1);
        } else {
            // If not, add the character to the map with a count of 1
            charCountMap.set(char, 1);
        }
    }

    // Sort the map by character counts in descending order
    const sortedCharCount = new Map([...charCountMap.entries()].sort((a, b) => b[1] - a[1]));

    // Return the sorted map
    return sortedCharCount;
}

function countAndDisplayCharacters(inputString, resultTableId, sourceStringId) {
    const result = countAndSortCharacters(inputString);


    const container = document.getElementById(resultTableId);

    let highlightedCharDiv = null; // To keep track of the previously clicked charDiv



    // Populate the row with cells for each character and its frequency
    var first = true;
    result.forEach((count, char) => {
        const charDiv = document.createElement('div');
        charDiv.textContent = `${char}: ${persianizeNumerals(count.toString())}`;
        charDiv.className = 'charCell';

        // Add a click event listener to highlight the character in the source string
        charDiv.addEventListener('click', (event) => {
            if (event.target.classList.contains('charCell')) {
                // Roll back the color of the previously clicked charDiv
                if (highlightedCharDiv) {
                    highlightedCharDiv.classList.remove('background-red');
                }

                const sourceStringDiv = document.getElementById(sourceStringId);
                const highlightedString = highlightCharacter(inputString, char, sourceStringDiv.innerHTML);
                sourceStringDiv.innerHTML = highlightedString;

                // Toggle the color of the clicked charDiv
                charDiv.classList.toggle('background-red');
                highlightedCharDiv = charDiv; // Update the reference to the clicked charDiv
            }
        });

        // Highlight the first character by default
        if (first) {
            first = false;
            charDiv.classList.add('background-red');
            highlightedCharDiv = charDiv;
            const sourceStringDiv = document.getElementById(sourceStringId);
            const highlightedString = highlightCharacter(inputString, char, sourceStringDiv.innerHTML);
            sourceStringDiv.innerHTML = highlightedString;
        }

        // Append the div to the container
        container.appendChild(charDiv);
    });


}

function highlightCharacter(previousHighlightedString, charToHighlight) {
    // Escape special characters in the regex pattern
    const escapedChar = charToHighlight.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

    // Create a regular expression to match the specified character
    const regex = new RegExp(escapedChar, 'g');

    // Remove previous highlighting
    const unhighlightedString = previousHighlightedString.replace(/<span style="color: red;">|<\/span>/g, '');

    // Replace the matched character with the HTML code for red color
    const highlightedString = unhighlightedString.replace(regex, `<span style="color: red;">$&</span>`);

    return highlightedString;
}

function inlineHighlight() {
    var searchTerm = document.getElementById('inline-search').value;
    if (searchTerm.trim().length == 0) {
        inlineUnHighlight();
        return;
    }
    var context = document.getElementById('garticle');

    $(context).unmark({
        done: function () {
            $(context).mark(searchTerm, {
                ignoreJoiners: true,
                separateWordSearch: false,
                ignorePunctuation: 'ًٌٍَُّ.،!؟ٔ:؛;*)([]«»ْ'.split(''),
                "exclude": [
                    "#poet-image *",
                    "#page-hierarchy *",
                    "#utils-navbar *",
                ],
                done: function (markCount) {
                    inlineSearchResultIndex = 0;
                    if (markCount == 0) {
                        document.getElementById('inline-search').classList.add("background-red");
                    }
                    else {
                        document.getElementById('inline-search').classList.remove("background-red");
                        inlineSearchResults = $("#garticle").find("mark");
                        jumpToInlineSearchResult();
                    }
                    $('#inline-search-matches').text(persianizeNumerals((inlineSearchResultIndex + 1).toString()) +' از '+ persianizeNumerals(markCount.toString()));
                }
            });
        }
    });
}

function inlineUnHighlight() {
    document.getElementById('inline-search').value = '';
    document.getElementById('fake-inline-div').value = '';
    document.getElementById('inline-search').classList.remove("background-red");
    $('#inline-search-matches').text('');
    var context = document.getElementById('garticle');
    $(context).unmark();
    document.getElementById('fake-inline-div').style.display = 'block';
    document.getElementById('main-inline-div').style.display = 'none';
    document.getElementById('fake-inline-search').value = '';
    document.getElementById('fake-inline-search').focus();
}

function displayMainInlineSearch() {
    document.getElementById('fake-inline-div').style.display = 'none';
    document.getElementById('main-inline-div').style.display = 'block';
    var txt = document.getElementById('fake-inline-search').value;
    document.getElementById('inline-search').value = txt;
    document.getElementById('inline-search').setSelectionRange(txt.length, txt.length);
    document.getElementById('inline-search').focus();
}

function jumpToInlineSearchResult() {
    if (inlineSearchResults.length) {
        var position,
            $current = inlineSearchResults.eq(inlineSearchResultIndex);
        inlineSearchResults.removeClass(currentClass);
        if ($current.length) {
            $current.addClass(currentClass);
            position = $current.offset().top - offsetTop;
            window.scrollTo(0, position);
            $('#inline-search-matches').text(persianizeNumerals((inlineSearchResultIndex + 1).toString()) + ' از ' + persianizeNumerals(inlineSearchResults.length.toString()));
        }
    }
}

function nextInlineSearchResult() {
    if (inlineSearchResultIndex < (inlineSearchResults.length - 1)) {
        inlineSearchResultIndex++;
    }
    jumpToInlineSearchResult();
}

function prevInlineSearchResult() {
    if (inlineSearchResultIndex > 0) {
        inlineSearchResultIndex--;
    }
    jumpToInlineSearchResult();
}


function loadMoreQuotedForRelatedPoem(quoteRecordId, poemId, relatedPoemId, poetImageUrl, poetNickName, canEdit) {
    var buttonId = 'load-more-quoted-button-' + quoteRecordId;
    var loadButton = document.getElementById(buttonId);
    if (loadButton != null) {
        loadButton.remove();
    }
    var divId = 'more-quoted-placeholder-' + quoteRecordId;
    var divParent = document.getElementById(divId);
    var imgeId = 'load-more-quoted-loadingimg-' + quoteRecordId;
    divParent.innerHTML = divParent.innerHTML + '<img id="' + imgeId + '" src="/image/loading.gif" alt="بارگذاری  "/>';

    $.ajax({
        type: "GET",
        url: '?Handler=MoreQuotedPoemsForRelatedPoemPartial',
        data: {
            poemId: poemId,
            relatedPoemId: relatedPoemId, 
            poetImageUrl: poetImageUrl, 
            poetNickName: poetNickName,
            canEdit: canEdit
        },
        success: function (data) {

            var imgElement = document.getElementById(imgeId);
            imgElement.remove();

            $(data).appendTo(document.getElementById(divId));
        },
    });
}


function loadMoreQuotedPoems(poemId, skip, poetImageUrl, poetNickName, canEdit) {
    var loadButton = document.getElementById('load-more-quoted');
    if (loadButton != null) {
        loadButton.remove();
    }
    var divParent = document.getElementById('more-quoted-placeholder');
    var imgeId = 'load-more-quoted-loadingimg';
    divParent.innerHTML = divParent.innerHTML + '<img id="' + imgeId + '" src="/image/loading.gif" alt="بارگذاری  "/>';

    $.ajax({
        type: "GET",
        url: '?Handler=MoreQuotedPoemsPartial',
        data: {
            poemId: poemId,
            skip: skip,
            poetImageUrl: poetImageUrl,
            poetNickName: poetNickName,
            canEdit: canEdit
        },
        success: function (data) {

            var imgElement = document.getElementById(imgeId);
            imgElement.remove();

            $(data).appendTo(document.getElementById('more-quoted-placeholder'));
        },
    });
}

function markAsTextOriginal(bookId, categoryId, bookName, catName) {
    if (!confirm('آیا ' + bookName + ' منبع کاغذی بخش ' + catName + ' است؟'))
        return;

    var url = '?handler=MarkAsTextOriginal';

    $.ajax({
        type: "POST",
        url: url,
        data: {
            bookId: bookId,
            categoryId: categoryId
        },
        success: function () {
            alert('فرایند کار شروع شد.');
        },
    });
}

function deleteRelatedImage(relatedImageType, linkId, altText) {
    if (!confirm('آیا از قطع ارتباط ' + altText + ' با این بخش اطمینان دارید؟'))
        return;
    var removeItemLink = false;
    if (relatedImageType == 0) {
        if (!confirm('لینک از طرف گنجینه هم قطع شود؟')) {
            removeItemLink = true;
        }
            
    }
    var url = '?handler=RelatedImageLink';

    $.ajax({
        type: "DELETE",
        url: url,
        data: {
            relatedImageType: relatedImageType,
            linkId: linkId,
            removeItemLink: removeItemLink
        },
        error: function (err) {
            alert('RelatedImageLink: ' + err.toString());
        },
        success: function () {
            alert('انجام شد.');
            location.reload();
        },
    });
}

function reloadWordCounts(catId, poetId, remStopWords) {
    var divParent = document.getElementById('wordcounts-placeholder');
    divParent.innerHTML = '<div id="wordcounts-placeholder"><div>';
    loadWordCounts(catId, poetId, remStopWords);
}

function switchTabWordsForPoem(evt, tabId, id, catId, poetId) {
    var loadButton = document.getElementById("load-word-counts");
    if (loadButton != null) {
        loadButton.remove();
        countPoemWords(id, catId, poetId);
    }

    switchTab(evt, tabId);
}
function switchTabWords(evt, tabId, catId, poetId) {
    var loadButton = document.getElementById("load-word-counts");
    if (loadButton != null) {
        loadButton.remove();
        loadWordCounts(catId, poetId, false);
    }
    
    switchTab(evt, tabId);
}
function loadWordCounts(catId, poetId, remStopWords) {
    var divParent = document.getElementById('wordcounts-placeholder');
    var imgElementId = 'loadingwordcountsimg';
    divParent.innerHTML = divParent.innerHTML + '<div class="bnumdiv" id="remove-this-wordcounts"><img id="' + imgElementId + '" src="/image/loading.gif" alt="بارگذاری"/></div>';
    $.ajax({
        type: "GET",
        url: '?Handler=CategoryWordCounts&catId=' + String(catId) + '&poetId=' + String(poetId) + '&remStopWords=' + remStopWords.toString(),
        error: function () {
            if (document.getElementById("remove-this-wordcounts") != null) {
                document.getElementById("remove-this-wordcounts").remove();
            }
        },
        success: function (data) {
            if (document.getElementById("remove-this-wordcounts") != null) {
                document.getElementById("remove-this-wordcounts").remove();
                if (document.getElementById("load-word-counts") != null) {
                    document.getElementById("load-word-counts").remove();
                }
                $(data).appendTo(divParent);
                plotChart('words-stats');
            }
        },
    });
}

function onSearchWordCounts(catId, poetId, totalWordCount) {
    setTimeout(function () {
        var value = document.getElementById('wordcountterm').value;
        if (value != null && value.trim() != '') {
            document.getElementById('remStopWords').style.display = 'none'
        }
        else {
            document.getElementById('remStopWords').style.display = 'block'
        }
        var divParent = document.getElementById('wordcounts-table');
        var imgElementId = 'loadingwordcountsimg';
        divParent.innerHTML = '<div class="bnumdiv" id="remove-this-wordcounts"><img id="' + imgElementId + '" src="/image/loading.gif" alt="بارگذاری"/></div>';
        $.ajax({
            type: "GET",
            url: '?Handler=SearchCategoryWordCounts&catId=' + String(catId) + '&poetId=' + String(poetId) + '&totalWordCount=' + String(totalWordCount) + '&term=' + value,
            error: function () {
                if (document.getElementById("remove-this-wordcounts") != null) {
                    document.getElementById("remove-this-wordcounts").remove();
                }
            },
            success: function (data) {
                if (document.getElementById("remove-this-wordcounts") != null) {
                    document.getElementById("remove-this-wordcounts").remove();
                    $(data).appendTo(divParent);
                    plotChart('words-stats');
                }
            },
        });

    }, 500);
}

function loadWordCountsByCat(term, poetId, catId, blur) {
    var divParent = document.getElementById('wordcounts-placeholder');
    var imgElementId = 'loadingwordcountsimg';
    divParent.innerHTML = divParent.innerHTML + '<div class="loadingcontainer" id="remove-this-wordcounts"><img id="' + imgElementId + '" src="/image/loading.gif" alt="بارگذاری"/></div>';
    $.ajax({
        type: "GET",
        url: '?Handler=WordCountsByPoet&term=' + term + '&poetId=' + poetId.toString() + '&catId=' + catId.toString() + '&blur=' + blur.toString(),
        error: function () {
            if (document.getElementById("remove-this-wordcounts") != null) {
                document.getElementById("remove-this-wordcounts").remove();
            }
        },
        success: function (data) {
            if (document.getElementById("remove-this-wordcounts") != null) {
                document.getElementById("remove-this-wordcounts").remove();
                if (document.getElementById("load-word-counts") != null) {
                    document.getElementById("load-word-counts").style.display = 'none';
                }
                $(data).appendTo(divParent);
                plotChart('words-stats');
            }
        },
    });
}


function countPoemWords(poemId) {
    var loadButton = document.getElementById("load-word-counts");
    if (loadButton != null) {
        loadButton.remove();
    }
    var divParent = document.getElementById('wordcounts-placeholder');
    var imgElementId = 'loadingwordcountsimg';
    divParent.innerHTML = divParent.innerHTML + '<div class="bnumdiv" id="remove-this-wordcounts"><img id="' + imgElementId + '" src="/image/loading.gif" alt="بارگذاری"/></div>';
    $.ajax({
        type: "GET",
        url: '?Handler=PoemWordCounts&poemId=' + String(poemId),
        error: function () {
            if (document.getElementById("remove-this-wordcounts") != null) {
                document.getElementById("remove-this-wordcounts").remove();
            }
        },
        success: function (data) {
            if (document.getElementById("remove-this-wordcounts") != null) {
                document.getElementById("remove-this-wordcounts").remove();
                divParent.innerHTML = '<div id="wordcounts-placeholder"><div>'
                $(data).appendTo(divParent);
                plotChart('words-stats');
            }
        },
    });
}

function switchToCatTab(evt, tabId, catId) {
    loadCatRecitations(catId);
    switchTab(evt, tabId);
}
function switchTab(evt, tabId) {
    // Declare all variables
    var i, tabcontent, tablinks;

    // Get all elements with class="tabcontent" and hide them
    tabcontent = document.getElementsByClassName("tabcontent");
    for (i = 0; i < tabcontent.length; i++) {
        tabcontent[i].style.display = "none";
    }

    // Get all elements with class="tablinks" and remove the class "active"
    tablinks = document.getElementsByClassName("tablinks");
    for (i = 0; i < tablinks.length; i++) {
        tablinks[i].className = tablinks[i].className.replace(" active", "");
    }

    // Show the current tab, and add an "active" class to the button that opened the tab
    document.getElementById(tabId).style.display = "block";
    evt.currentTarget.className += " active";
} 

function persianToEnglishNumber(str) {
    str = str.replace(/٬/g, '');
    const persianDigits = '۰۱۲۳۴۵۶۷۸۹';
    return str.replace(/[۰-۹]/g, d => persianDigits.indexOf(d));
}

function plotChart(tableId, maxCols = 9) {


    let table = document.getElementById(tableId);
    if (!table) return;

    let labels = [];
    let values = [];

    let headerCols = table.querySelector("thead tr").children;
    let xTitle = headerCols[1].innerText.trim();
    let yTitle = headerCols[2].innerText.trim();
    table.querySelectorAll("tbody tr").forEach(row => {
        let cols = row.querySelectorAll("td");
        if (cols.length >= 3) {
            if (values.length > maxCols) return;
            let rowNumber = persianToEnglishNumber(cols[0].innerText.trim());
            if (rowNumber === "0") return; // Ignore rows with row number ۰

            let xValue = persianToEnglishNumber(cols[1].innerText.trim());
            let yValue = persianToEnglishNumber(cols[2].innerText.trim());

            labels.push(xValue);
            values.push(parseInt(yValue));
            

        }
    });

    let existingCanvas = document.getElementById(`chart-${tableId}`);
    if (existingCanvas) {
        existingCanvas.remove();
    }

    let canvas = document.createElement("canvas");
    canvas.id = `chart-${tableId}`;
    table.parentNode.insertBefore(canvas, table.nextSibling);

    const isDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    const textColor = isDark ? '#ffffff' : '#333333';


    new Chart(canvas, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: yTitle,
                data: values,
                backgroundColor: 'rgba(54, 162, 235, 0.5)',
                borderColor: 'rgba(54, 162, 235, 1)',
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            scales: {
                x: {
                    title: {
                        display: true,
                        text: xTitle,
                        font: { family: 'Vazirmatn', size: 14 },
                        color: textColor
                    },
                    ticks: {
                        font: { family: 'Vazirmatn', size: 12 },
                        color: textColor
                    }
                },
                y: {
                    title: {
                        display: true,
                        text: yTitle,
                        font: { family: 'Vazirmatn', size: 14 },
                        color: textColor
                    },
                    ticks: {
                        font: { family: 'Vazirmatn', size: 12 },
                        color: textColor
                    },
                    beginAtZero: true,
                }
            },
            plugins: {
                legend: {
                    display: false,
                },
            },
            layout: {
                padding: 10
            }
        }
    });
}

function deletePoetFromSearch(poetId, poetName) {
    if (!confirm('آیا می‌خواهید ' + poetName + ' را از نتایج حذف کنید؟')) return;
    const currentUrl = new URL(window.location.href);
    const params = currentUrl.searchParams;
    params.append('e', poetId.toString());
    currentUrl.search = params.toString();
    window.location.href = currentUrl.toString();
}

function loadContributions(dataType) {
    var divParent = document.getElementById('days-placeholder');
    divParent.innerHTML = '<div class="bnumdiv" id="remove-days-placeholder"><img src="/image/loading.gif" alt="بارگذاری"/></div>';
    $.ajax({
        type: "GET",
        url: '?Handler=GroupedByDate&dataType=' + dataType,
        error: function (e) {
            if (document.getElementById("remove-days-placeholder") != null) {
                document.getElementById("remove-days-placeholder").remove();
            }
            if (e.responseText == null)
                alert(e);
            else
                alert(e.responseText);
        },
        success: function (data) {
            if (document.getElementById("remove-days-placeholder") != null) {
                document.getElementById("remove-days-placeholder").remove();
                $(data).appendTo(divParent);
                plotChart('grouped-by-date', 100);
                plotChart('grouped-by-user', 100);

                var elements = document.querySelectorAll('.poemtablinks.active');
                for (var i = 0; i < elements.length; i++) {
                    elements[i].classList.remove('active');
                }
                var dataTypeButton = document.getElementById(dataType);
                if (dataTypeButton) {
                    dataTypeButton.classList.add('active');
                }
            }
        },
    });
}
function loadContributionsDays(dataType, pageNumber) {
    var divParent = document.getElementById('days');
    divParent.innerHTML = '<div class="bnumdiv" id="remove-days-placeholder"><img src="/image/loading.gif" alt="بارگذاری"/></div>';
    $.ajax({
        type: "GET",
        url: '?Handler=GroupedByDay&dataType=' + dataType + '&pageNumber=' + pageNumber.toString(),
        error: function (e) {
            if (document.getElementById("remove-days-placeholder") != null) {
                document.getElementById("remove-days-placeholder").remove();
            }
            if (e.responseText == null)
                alert(e);
            else
                alert(e.responseText);
        },
        success: function (data) {
            if (document.getElementById("remove-days-placeholder") != null) {
                document.getElementById("remove-days-placeholder").remove();
                $(data).appendTo(divParent);
                plotChart('grouped-by-date', 100);
            }
        },
    });
}

function loadContributionsUsers(dataType, pageNumber) {
    var divParent = document.getElementById('usrs');
    divParent.innerHTML = '<div class="bnumdiv" id="remove-days-placeholder"><img src="/image/loading.gif" alt="بارگذاری"/></div>';
    $.ajax({
        type: "GET",
        url: '?Handler=GroupedByUsers&dataType=' + dataType + '&pageNumber=' + pageNumber.toString(),
        error: function (e) {
            if (document.getElementById("remove-days-placeholder") != null) {
                document.getElementById("remove-days-placeholder").remove();
            }
            if (e.responseText == null)
                alert(e);
            else
                alert(e.responseText);
        },
        success: function (data) {
            if (document.getElementById("remove-days-placeholder") != null) {
                document.getElementById("remove-days-placeholder").remove();
                $(data).appendTo(divParent);
                plotChart('grouped-by-user', 100);
            }
        },
    });
}