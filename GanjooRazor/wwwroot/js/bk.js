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
    divParent.innerHTML = divParent.innerHTML + '<div class="bnumdiv" id="' + divId + '"><img id="' + imgElementId +'" src="/image/loading.gif" alt="بارگذاری"/></div>';
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



function btshmr_internal(poemId) {
    var bnum = getElements("bnum");
    if (bnum.length != 0) {
        for (var i = 0; i < bnum.length; ++i) {
            bnum[i].remove();
        }
        var bnumdiv = getElements("bnumdiv");
        for (var i = 0; i < bnumdiv.length; ++i) {
            bnumdiv[i].remove();
        }
        document.getElementById("bnum-button").innerHTML = 'شماره‌گذاری<i class="info-buttons" id="format_list_numbered_rtl">format_list_numbered_rtl</i>';
        return true;
    }
    document.getElementById("bnum-button").innerHTML = 'حذف شماره‌ها<i class="info-buttons" id="format_list_numbered_rtl">format_list_numbered_rtl</i>';
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
    return true;
}

function btshmr(poemId) {
    setTimeout(function () { btshmr_internal(poemId); }, 1);
}

function switchPlayerScrollLock() {
    playerScrollLock = !playerScrollLock;
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
    }
    else {
        const lockButtons = document.querySelectorAll('.recitation-scrolllock');
        lockButtons.forEach(function (lockButtons) {
            lockButtons.classList.toggle('recitation-scrollunlock');
            lockButtons.classList.toggle('recitation-scrolllock');
        });
    }

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

function fillnarrations(coupletIndex) {
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
                        var audioControl = document.getElementById('audio-' + String(recitationOrder));
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

function editCouplet(poemId, coupletIndex) {

    var vIndex = getVerseIndexFromCoupleIndex(coupletIndex);
    location.href = '/User/Editor?id=' + poemId + '#id-' + String(vIndex + 1);
}

function switchBookmark(poemId, coupletIndex) {
    var iconElementId = 'bookmark-icon-' + String(coupletIndex);
    if (document.getElementById(iconElementId) != null) {
        document.getElementById(iconElementId).innerHTML = 'star_half';
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
            alert(err)
        },
        success: function (isBookmarked) {
            if (isBookmarked == true) {
                if (document.getElementById(iconElementId) != null) {
                    document.getElementById(iconElementId).innerHTML = 'star';
                }

                if (coupletIndex == 0) {
                    document.getElementById('bookmark').innerHTML = 'نشان شده<i class="info-buttons" id="bookmark-icon">star</i>';
                }
            }
            else {
                if (document.getElementById(iconElementId) != null) {
                    document.getElementById(iconElementId).innerHTML = 'star_border';
                }

                if (coupletIndex == 0) {
                    document.getElementById('bookmark').innerHTML = 'نشان کردن<i class="info-buttons" id="bookmark-icon">star_border</i>';
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
            url: '?Handler=IsCoupletBookmarked&poemId=' + String(poemId) + '&coupletIndex=0',
            error: function (err) {
                console.log(err);
            },
            success: function (isBookmarked) {
                if (isBookmarked) {
                    document.getElementById('bookmark').innerHTML = 'نشان شده<i class="info-buttons" id="bookmark-icon">star</i>';
                }
                else {
                    document.getElementById('bookmark').innerHTML = 'نشان کردن<i class="info-buttons" id="bookmark-icon">star_border</i>';
                }
            },
        });
    }, 1);
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
        console.log('Error sharing: ' + error);
    }
}

function copyPoemText() {
    var text = getPoemText();
    navigator.clipboard.writeText(text);
    var tooltip = document.getElementById("copytext-tooltip");
    tooltip.innerHTML = "متن در حافظه رونوشت شد.";
}

function copyPoemLink() {
    var url = window.location.href;
    if (url.indexOf('#') != -1) {
        url = url.substring(0, url.indexOf('#'));
    }
    navigator.clipboard.writeText(url);
    var tooltip = document.getElementById("copylink-tooltip");
    tooltip.innerHTML = "نشانی در حافظه رونوشت شد.";
}

function copyCoupletUrl(coupletIndex) {
    var url = window.location.href;
    if (url.indexOf('#') != -1) {
        url = url.substring(0, url.indexOf('#'));
    }
    url += ( '#bn' + String(coupletIndex + 1));
    navigator.clipboard.writeText(url);
    var tooltip = document.getElementById("copylink-tooltip-" + String(coupletIndex));
    tooltip.innerHTML = "نشانی در حافظه رونوشت شد.";
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
        console.log('Error sharing: ' + error);
    }
}

function copyCoupletText(coupletIndex) {
    var text = getCoupletText(coupletIndex);
    navigator.clipboard.writeText(text);
    var tooltip = document.getElementById("copytext-tooltip-" + String(coupletIndex));
    tooltip.innerHTML = "متن در حافظه رونوشت شد.";
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

function copyCommentUrl(commentId, divSuffix) {
    var url = window.location.href;
    if (url.indexOf('#') != -1) {
        url = url.substring(0, url.indexOf('#'));
    }
    url += ('#comment-' + String(commentId));
    navigator.clipboard.writeText(url);
    var tooltip = document.getElementById("copycommentlink-tooltip-" + String(commentId) + divSuffix);
    tooltip.innerHTML = "نشانی در حافظه رونوشت شد.";
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

            $(data).appendTo(document.getElementById('more-related-placeholder-' + String(sectionIndex) ));
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
    document.getElementById('load-all-recitations').style.display = 'none';
}

function switch_section(sectionId, buttonId){
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
                alert(e);
            else
                alert(e.responseText);
        },
    });
}

function MarkUserUpvotedRecitations(poemId) {
    setTimeout(function () {
        $.ajax({
            type: "GET",
            url: '?Handler=UserUpvotedRecitations&poemId=' + String(poemId),
            error: function (err) {
                console.log(err);
            },
            success: function (result) {
                for (var i = 0; i < result.length; i++) {
                    document.getElementById('recitaion-' + String(result[i])).classList.add('recitation-vote');
                }
            },
        });
    }, 1);
}

function onInlineSearch(value, resultBlockId, itemsClass) {
    const foundPoetsNode = document.getElementById(resultBlockId);
    foundPoetsNode.innerHTML = '';
    if (value.length > 0) {
        let replaced = value.replace(/0/gi, "۰").replace(/1/gi, "۱").replace(/2/gi, "۲")
            .replace(/3/gi, "۳").replace(/4/gi, "۴").replace(/5/gi, "۵")
            .replace(/6/gi, "۶").replace(/7/gi, "۷").replace(/8/gi, "۸")
            .replace(/9/gi, "۹");
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
            error: function (err) {
                console.log(err);
            },
        });
    }, 1);
}

function CheckIfHasNotificationsForHomePage() {
    setTimeout(function () {
        $.ajax({
            type: "GET",
            url: '?Handler=CheckIfHasNotifications',
            error: function (err) {
                console.log(err);
            },
            success: function (result) {
                if (result != '') {
                    document.getElementById('notification-badge').classList.toggle('hidden-recitation');
                    document.getElementById('notification-badge').classList.toggle('visible-notification-badge');
                }
            },
        });
    }, 1);
}