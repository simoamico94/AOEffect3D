import { connect, createDataItemSigner } from '@permaweb/aoconnect'

export async function sendMessage(pid, data, action)
{
    const messageId = await connect().message({
        process: pid,
        signer: createDataItemSigner(globalThis.arweaveWallet),
        tags: [{ name: 'Action', value: action }],
        data: data
    });

    const result = await connect().result({
        message: messageId,
        process: pid
    });

    console.log(result);

    if (result.Error)
    {
        myUnityInstance.SendMessage('AOConnectManager', 'MessageCallback', result.Error);
        throw new Error(result.Error)
    }

    if (result.Output?.data?.json)
    {
        myUnityInstance.SendMessage('AOConnectManager', 'MessageCallback', result.Output?.data?.json);
        return result.Output?.data?.json
    }

    return undefined;
}
