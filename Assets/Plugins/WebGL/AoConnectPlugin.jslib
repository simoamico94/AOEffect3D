
mergeInto(LibraryManager.library, {
    evaluateUnity: function (pidPtr, dataPtr) {
        console.log("loading");
        var pid = UTF8ToString(pidPtr);
        var data = UTF8ToString(dataPtr);
        return evaluate(pid, data);
    }
});
