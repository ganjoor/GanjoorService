$(function () {
    var $url = $("#trackurl");
    var $spinner = $("#fetching-metadata");
    var lastLookedUp = "";

    function fillIfEmpty(selector, value) {
        var $input = $(selector);
        if (value && $input.val().trim() === "") {
            $input.val(value);
        }
    }

    function fetchMetadata() {
        var url = $url.val().trim();
        if (url === "" || url === lastLookedUp) {
            return;
        }
        lastLookedUp = url;
        $spinner.show();
        $.ajax({
            type: "POST",
            url: "/musiclink?handler=FetchMetadata",
            data: { url: url }
        })
            .done(function (data) {
                fillIfEmpty("#PoemMusicTrackViewModel_TrackName", data.trackName);
                fillIfEmpty("#PoemMusicTrackViewModel_ArtistName", data.artistName);
            })
            .always(function () {
                $spinner.hide();
            });
    }

    $url.on("blur", fetchMetadata);
    $url.on("paste", function () {
        window.setTimeout(fetchMetadata, 0);
    });
});
