mergeInto(LibraryManager.library, {
    GetURLFromQueryStr: function ()
    {
        var returnStr = window.top.location.href;
        var bufferSize = lengthBytesUTF8(returnStr) + 1
        var buffer = _malloc(bufferSize);
        stringToUTF8(returnStr, buffer, bufferSize);
        return buffer;
    },

    SendMessageJS: function (pidPtr, dataPtr, actionPtr)
    {
        var pid = UTF8ToString(pidPtr);
        var data = UTF8ToString(dataPtr);
        var action = UTF8ToString(actionPtr);

        UnityAO.sendMessage(pid, data, action);
    },

    ConnectWalletJS: function ()
    {
        UnityAO.connectArweaveWallet();
    },

    FetchProcessesJS: function (addressPtr)
    {
        var address = UTF8ToString(addressPtr);

        UnityAO.fetchProcesses(address);
    },

    SpawnProcessJS: function (pidPtr)
    {
        var pid = UTF8ToString(pidPtr);

        UnityAO.spawnProcess(pid);
    },

    AlertMessageJS: function (messagePtr)
    {
        var message = UTF8ToString(messagePtr);

        alert(message);
    },
    //LoadLuaJS: function (pidPtr, luaPtr) {
    //    var pid = UTF8ToString(pidPtr);
    //    var lua = UTF8ToString(luaPtr);

    //    UnityAO.loadLua(pid, lua);
    //},
});

