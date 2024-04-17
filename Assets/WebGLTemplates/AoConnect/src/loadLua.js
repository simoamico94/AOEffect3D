import { message, createDataItemSigner } from "@permaweb/aoconnect";

export async function loadLua(process, lua)
{
    try
    {
        const messageID = await message({
            process: process,
            tags: [{ name: "Action", value: "Eval" }],
            signer: createDataItemSigner(globalThis.arweaveWallet),
            data: lua,
        });

        console.log(messageID);
    }
    catch (error)
    {
        messageID = 'Error';
        console.error(error);
    }
    finally
    {
        myUnityInstance.SendMessage('AOConnectManager', 'LuaCallback', messageID);
    }
}