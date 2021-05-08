$(function () {


    $('#collection').on('change', function () {
        if ($("#collection option:selected").text() != '') {
            $("#program").attr("disabled", "disabled");
            $.post("/golha?handler=FillPrograms", {
                'collection': $("#collection option:selected").val()
            },
                function (data) {
                    var sel = $("#program");
                    sel.empty();
                    for (var i = 0; i < data.length; i++) {
                        sel.removeAttr("disabled");
                        sel.append('<option program-url="' + data[i].url + '" value="' + data[i].id + '">' + data[i].title + '</option>');
                    }
                    $("#program").removeAttr("disabled");
                    $('#program').trigger("change");

                    var collectionlink = $("#collectionlink");
                    collectionlink.attr('href', 'http://www.golha.co.uk/fa/search_basic/' + $("#collection option:selected").val());
                    $('#l1').css('visibility', 'visible');

                }, "json");
        }
    });



    $('#program').on('change', function () {
        if ($("#program option:selected").text() != '') {
            $("#track").attr("disabled", "disabled");
            $("#programlink").attr('href', $("#program option:selected").attr('program-url'));
            $('#l2').css('visibility', 'visible');

            $.post("/golha?handler=FillTracks", {
                'program': $("#program option:selected").val()
            },
                function (data) {
                    var sel = $("#track");
                    sel.empty();
                    for (var i = 0; i < data.length; i++) {
                        sel.removeAttr("disabled");
                        sel.append('<option value="' + data[i].id + '">' + data[i].timing + ' ' + data[i].title + '</option>');
                    }
                    $("#track").removeAttr("disabled");
                    $('#track').trigger("change");

                }, "json");
        }
    });

    $('#track').on('change', function () {
        if ($("#track option:selected").text() != '') {

            $("#PoemMusicTrackViewModel_GolhaTrackId").val($("#track option:selected").val());

        }
    });

});