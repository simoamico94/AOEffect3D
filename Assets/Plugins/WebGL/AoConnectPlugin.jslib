
mergeInto(LibraryManager.library, {
    sendMessageUnity: function (pidPtr, dataPtr) {
        console.log("loading");
        var pid = UTF8ToString(pidPtr);
        var data = UTF8ToString(dataPtr);
        return UnityAO.sendMessage(pid, data);
    }
});
