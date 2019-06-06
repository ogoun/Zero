$(function () {
    $("#wordapisample").text("/api/text/words?text=...");
    $("#wordsplittype").change(function () {
        switch ($("#wordsplittype").val()) {
            case "1":
                $("#wordapisample").text("/api/text/words?text=...");
                break
            case "2":
                $("#wordapisample").text("/api/text/words/unique?text=...");
                break
            case "3":
                $("#wordapisample").text("/api/text/words/clean?text=...");
                break
        }
    });

    $("#stemapisample").text("/api/stem?text=...");
    $("#stemsplittype").change(function () {
        switch ($("#stemsplittype").val()) {
            case "1":
                $("#stemapisample").text("/api/stem?text=...");
                break
            case "2":
                $("#stemapisample").text("/api/stem/unique?text=...");
                break
            case "3":
                $("#stemapisample").text("/api/stem/clean?text=...");
                break
        }
    });

    $("#lemmapisample").text("/api/lemma?text=...");
    $("#lemmtype").change(function () {
        switch ($("#lemmtype").val()) {
            case "1":
                $("#lemmapisample").text("/api/lemma?text=...");
                break
            case "2":
                $("#lemmapisample").text("/api/lemma/unique?text=...");
                break
            case "3":
                $("#lemmapisample").text("/api/lemma/clean?text=...");
                break
        }
    });

    $("#wordssearchapisample").text("/api/text/occurences/words?text=...&words=[...,...]");
    $("#wordssearchtype").change(function () {
        var usePost = $("#wordssearchusepost").prop("checked");
        switch ($("#wordssearchtype").val()) {
            case "1":
                if (usePost)
                    $("#wordssearchapisample").text("/api/text/occurences/words with WordsSearchRequest(string Text, string[] Words) in BODY");
                else
                    $("#wordssearchapisample").text("/api/text/occurences/words?text=...&words=[...,...]");
                break
            case "2":
                if (usePost)
                    $("#wordssearchapisample").text("/api/stem/occurences/words with WordsSearchRequest(string Text, string[] Words) in BODY");
                else
                    $("#wordssearchapisample").text("/api/stem/occurences/words?text=...&words=[...,...]");
                break
            case "3":
                if (usePost)
                    $("#wordssearchapisample").text("api/lemma/occurences/words with WordsSearchRequest(string Text, string[] Words) in BODY");
                else
                    $("#wordssearchapisample").text("api/lemma/occurences/words?text=...&words=[...,...]");
                break
        }
    });

    $("#phrasessearchapisample").text("/api/text/occurences/phrases?text=...&words=[...,...]");
    $("#phrasessearchtype").change(function () {
        var usePost = $("#phrasessearchusepost").prop("checked");
        switch ($("#phrasessearchtype").val()) {
            case "1":
                if (usePost)
                    $("#phrasessearchapisample").text("/api/text/occurences/phrases with WordsSearchRequest(string Text, string[] Words) in BODY");
                else
                    $("#phrasessearchapisample").text("/api/text/occurences/phrases?text=...&words=[...,...]");
                break
            case "2":
                if (usePost)
                    $("#phrasessearchapisample").text("/api/stem/occurences/phrases with WordsSearchRequest(string Text, string[] Words) in BODY");
                else
                    $("#phrasessearchapisample").text("/api/stem/occurences/phrases?text=...&words=[...,...]");
                break
            case "3":
                if (usePost)
                    $("#phrasessearchapisample").text("api/lemma/occurences/phrases with WordsSearchRequest(string Text, string[] Words) in BODY");
                else
                    $("#phrasessearchapisample").text("api/lemma/occurences/phrases?text=...&words=[...,...]");
                break
        }
    });
});

/*----------------------------------------*/
/*-------------     MODELS     -----------*/
/*----------------------------------------*/
function LexToken(entry) {
    this.Word = entry.Word;
    this.Token = entry.Token;
    this.Position = entry.Position;
}

function LexTokenCollection(entry) {
    buf = []
    entry.forEach(function (item) {
        buf.push(new LexToken(item));
    });
    this.Items = buf;
}

function WordsOccurences(data) {
    var occurences = {};
    for (var key in data) {
        occurences[key] = new LexTokenCollection(data[key]);
    }
    this.Occurences = occurences;
}

function PhrasesOccurences(data) {
    var occurences = {};
    for (var key in data) {
        occurences[key] = [];
        data[key].forEach(function (entry) {
            occurences[key].push(new LexTokenCollection(entry));
        });
    }
    this.Occurences = occurences;
}
/*----------------------------------------*/
/*------------- TEXT TO TOKENS -----------*/
/*----------------------------------------*/
function getWords() {
    var usePost = $("#wordsusepost").prop("checked");
    var text = $("#primitivetext").val();
    switch ($("#wordsplittype").val()) {
        case '1':
            requestTokens(usePost, "/api/text/words", text, function (list) {
                $("#primitiveout").val(list.map(e=>e.Word).join(", "));
            }, "#wordaddinfo");
            break;
        case '2':
            requestTokens(usePost, "/api/text/words/unique", text, function (list) {
                $("#primitiveout").val(list.map(e=>e.Word).join(", "));
            }, "#wordaddinfo");
            break;
        case '3':
            requestTokens(usePost, "/api/text/words/clean", text, function (list) {
                $("#primitiveout").val(list.map(e=>e.Word).join(", "));
            }, "#wordaddinfo");
            break;
    }
}

function getStems() {
    var usePost = $("#stemsusepost").prop("checked");
    var text = $("#stemtext").val();
    switch ($("#stemsplittype").val()) {
        case '1':
            requestTokens(usePost, "/api/stem", text, function (list) {
                $("#stemout").val(list.map(e=>e.Token).join(", "));
            }, "#stemaddinfo");
            break;
        case '2':
            requestTokens(usePost, "/api/stem/unique", text, function (list) {
                $("#stemout").val(list.map(e=>e.Token).join(", "));
            }, "#stemaddinfo");
            break;
        case '3':
            requestTokens(usePost, "/api/stem/clean", text, function (list) {
                $("#stemout").val(list.map(e=>e.Token).join(", "));
            }, "#stemaddinfo");
            break;
    }
}

function getLemms() {
    var usePost = $("#lemmausepost").prop("checked");
    var text = $("#lemmatext").val();
    switch ($("#lemmtype").val()) {
        case '1':
            requestTokens(usePost, "/api/lemma", text, function (list) {
                $("#lemmaout").val(list.map(e=>e.Token).join(", "));
            }, "#lemmaddinfo");
            break;
        case '2':
            requestTokens(usePost, "/api/lemma/unique", text, function (list) {
                $("#lemmaout").val(list.map(e=>e.Token).join(", "));
            }, "#lemmaddinfo");
            break;
        case '3':
            requestTokens(usePost, "/api/lemma/clean", text, function (list) {
                $("#lemmaout").val(list.map(e=>e.Token).join(", "));
            }, "#lemmaddinfo");
            break;
    }
}
/*----------------------------------------*/
/*------------- SEARCH IN TEXT -----------*/
/*----------------------------------------*/
function searchWordsInText() {
    var usePost = $("#wordssearchusepost").prop("checked");
    var text = $("#wordssearchtext").val();
    var words = $("#searchwords").val().split(' ');
    switch ($("#wordssearchtype").val()) {
        case '1':
            requestWordsOccurences(usePost, "api/text/occurences/words", text, words,
                function (result) {
                    var text = [];
                    for (var key in result.Occurences) {
                        text.push(key + " => " + result.Occurences[key].Items.map(e => e.Token + " (" + e.Position + ")").join(', '))
                    }
                    $("#wordssearchtextout").val(text.join('\r\n'));
                }, "#wordssearchaddinfo");
            break;
        case '2':
            requestWordsOccurences(usePost, "api/stem/occurences/words", text, words,
                function (result) {
                    var text = [];
                    for (var key in result.Occurences) {
                        text.push(key + " => " + result.Occurences[key].Items.map(e => e.Token + " (" + e.Word + " on " + e.Position + ")").join(', '))
                    }
                    $("#wordssearchtextout").val(text.join('\r\n'));
                }, "#wordssearchaddinfo");
            break;
        case '3':
            requestWordsOccurences(usePost, "api/lemma/occurences/words", text, words,
                function (result) {
                    var text = [];
                    for (var key in result.Occurences) {
                        text.push(key + " => " + result.Occurences[key].Items.map(e => e.Token + " (" + e.Word + " on " + e.Position + ")").join(', '))
                    }
                    $("#wordssearchtextout").val(text.join('\r\n'));
                }, "#wordssearchaddinfo");
            break;
    }
}

function searchPhrasesInText() {
    var usePost = $("#phrasessearchusepost").prop("checked");
    var text = $("#phrasessearchtext").val();
    var words = $("#searchphrases").val().split(';');
    switch ($("#phrasessearchtype").val()) {
        case '1':
            requestPhrasesOccurences(usePost, "api/text/occurences/phrases", text, words,
                function (result) {
                    var text = [];
                    for (var key in result.Occurences) {
                        result.Occurences[key].forEach(function (set) {
                            text.push(key + " => " + set.Items.map(e => e.Token + " (" + e.Position + ")").join(', '))
                        });
                    }
                    $("#phrasessearchtextout").val(text.join('\r\n'));
                }, "#phrasessearchaddinfo");
            break;
        case '2':
            requestPhrasesOccurences(usePost, "api/stem/occurences/phrases", text, words,
                function (result) {
                    var text = [];
                    for (var key in result.Occurences) {
                        result.Occurences[key].forEach(function (set) {
                            text.push(key + " => " + set.Items.map(e => e.Token + " (" + e.Position + ")").join(', '))
                        });
                    }
                    $("#phrasessearchtextout").val(text.join('\r\n'));
                }, "#phrasessearchaddinfo");
            break;
        case '3':
            requestPhrasesOccurences(usePost, "api/lemma/occurences/phrases", text, words,
                function (result) {
                    var text = [];
                    for (var key in result.Occurences) {
                        result.Occurences[key].forEach(function (set) {
                            text.push(key + " => " + set.Items.map(e => e.Token + " (" + e.Position + ")").join(', '))
                        });
                    }
                    $("#phrasessearchtextout").val(text.join('\r\n'));
                }, "#phrasessearchaddinfo");
            break;
    }
}
/*----------------------------------------*/
/*----------- AJAX FOR TOKENS ------------*/
/*----------------------------------------*/
function requestTokens(usePost, resource, data, callback, informer) {
    var mapper = function (val) {
        var set = [];
        val.forEach(function (entry) {
            set.push(new LexToken(entry));
        });
        return set;
    };
    var payload = { text: data };
    if (usePost)
        post(resource, data, mapper, callback, informer);
    else
        get(resource, payload, mapper, callback, informer);
}
/*----------------------------------------*/
/*------- AJAX FOR WORD OCCURENCES -------*/
/*----------------------------------------*/
function requestWordsOccurences(usePost, resource, data, search, callback, informer) {
    var mapper = function (val) { return new WordsOccurences(val); };
    var payload = { text: data, words: search };
    if (usePost)
        post(resource, payload, mapper, callback, informer);
    else
        get(resource, payload, mapper, callback, informer);
}
/*----------------------------------------*/
/*----- AJAX FOR PHRASES OCCURENCES ------*/
/*----------------------------------------*/
function requestPhrasesOccurences(usePost, resource, data, search, callback, informer) {
    var mapper = function (val) { return new PhrasesOccurences(val); };
    var payload = { text: data, words: search };
    if (usePost)
        post(resource, payload, mapper, callback, informer);
    else
        get(resource, payload, mapper, callback, informer);
}
/*----------------------------------------*/
/*-----        AJAX REQUESTS        ------*/
/*----------------------------------------*/
function get(resource, payload, mapper, callback, informer) {
    var ajaxTime = new Date().getTime();
    $.get(resource, payload, function (val) {
        var totalTime = new Date().getTime() - ajaxTime;
        $(informer).text(totalTime + " ms");
        callback(mapper(val));
    });
}

function post(resource, payload, mapper, callback, informer) {
    var ajaxTime = new Date().getTime();
    $.ajax({
        type: "POST",
        url: resource,
        contentType: "application/json; charset=utf-8",
        data: JSON.stringify(payload),
        success: function (val) {
            var totalTime = new Date().getTime() - ajaxTime;
            $(informer).text(totalTime + " ms");
            callback(mapper(val));
        }
    });
}