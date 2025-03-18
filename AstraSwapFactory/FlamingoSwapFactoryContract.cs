using System.ComponentModel;
using AstraSwapSwapFactory.Models;
using EpicChain;
using EpicChain.SmartContract.Framework;
using EpicChain.SmartContract.Framework.Native;
using EpicChain.SmartContract.Framework.Services;
using EpicChain.SmartContract.Framework.Attributes;

namespace AstraSwapSwapFactory
{
    [DisplayName("AstraSwap Factory Contract")]
    [ManifestExtra("Author", "AstraSwap Finance")]
    [ManifestExtra("Email", "devs@epic-chain.org")]
    [ManifestExtra("Description", "This contract serves as the core factory for AstraSwap, enabling the creation and management of liquidity pools, token swaps, and other DeFi functionalities on the AstraSwap decentralized exchange. It facilitates the deployment of automated market maker (AMM) mechanics, ensuring a seamless trading experience.")]
    [ManifestExtra("Version", "1.0.0")]
    [ManifestExtra("License", "MIT")]
    [ManifestExtra("Website", "https://astra.epic-chain.org")]
    [ContractPermission("*")] // Allows unrestricted access to all contract functionalities while avoiding potential issues with native contract hash changes.
    public partial class AstraSwapSwapFactoryContract : SmartContract
    {
        /// <summary>
        /// The storage area prefix of the transaction pair list is only allowed to be one byte
        /// </summary>
        private static readonly byte[] ExchangeMapKey = new byte[] { 0xff };

        /// <summary>
        /// Query the transaction pair contract, ByteString can be null, leave it to the caller to judge
        /// </summary>
        /// <param name="tokenA">Xep5 tokenA</param>
        /// <param name="tokenB">Xep5 tokenB</param>
        /// <returns></returns>
        public static ByteString GetExchangePair(UInt160 tokenA, UInt160 tokenB)
        {
            Assert(tokenA != tokenB, "Identical Address", tokenA);
            return StorageGet(GetPairKey(tokenA, tokenB));
        }

        /// <summary>
        /// Add exchange contract mapping of xep17 assets
        /// </summary>
        /// <param name="tokenA">Xep5 tokenA</param>
        /// <param name="tokenB">Xep5 tokenB</param>
        /// <param name="exchangeContractHash"></param>
        /// <returns></returns>
        public static bool CreateExchangePair(UInt160 tokenA, UInt160 tokenB, UInt160 exchangeContractHash)
        {
            Assert(Runtime.CheckWitness(GetAdmin()), "Forbidden");
            Assert(tokenA.IsAddress() && tokenB.IsAddress(), "Invalid Address");
            Assert(tokenA != tokenB, "Identical Address", tokenA);
            var key = GetPairKey(tokenA, tokenB);
            var value = StorageGet(key);
            Assert(value == null || value.Length == 0, "Exchange Already Existed");

            StoragePut(key, exchangeContractHash);
            onCreateExchange(tokenA, tokenB, exchangeContractHash);
            return true;
        }


        /// <summary>
        /// Add exchange contract mapping of xep17 assets
        /// </summary>
        /// <param name="exchangeContractHash"></param>
        /// <returns></returns>
        public static bool RegisterExchangePair(UInt160 exchangeContractHash)
        {
            Assert(Runtime.CheckWitness(GetAdmin()), "Forbidden");
            var contract = ContractManagement.GetContract(exchangeContractHash);
            Assert(contract != null, "Not Deployed");
            var token0 = (UInt160)Contract.Call(exchangeContractHash, "getToken0", CallFlags.ReadOnly, new object[0]);
            var token1 = (UInt160)Contract.Call(exchangeContractHash, "getToken1", CallFlags.ReadOnly, new object[0]);
            Assert(token0 != null && token1 != null, "Token Invalid");
            var key = GetPairKey(token0, token1);
            //var value = StorageGet(key);
            //if (value != null)
            //{
            //    onRemoveExchange(token0, token1);
            //}
            StoragePut(key, exchangeContractHash);
            onCreateExchange(token0, token1, exchangeContractHash);
            return true;
        }

        /// <summary>
        /// Delete the exchange contract mapping of xep17 assets
        /// </summary>
        /// <param name="tokenA"></param>
        /// <param name="tokenB"></param>
        /// <returns></returns>
        public static bool RemoveExchangePair(UInt160 tokenA, UInt160 tokenB)
        {
            Assert(tokenA.IsAddress() && tokenB.IsAddress(), "Invalid Address");
            Assert(Runtime.CheckWitness(GetAdmin()), "Forbidden");
            var key = GetPairKey(tokenA, tokenB);
            var value = StorageGet(key);
            if (value?.Length > 0)
            {
                StorageDelete(key);
                onRemoveExchange(tokenA, tokenB);
            }
            return true;
        }



        /// <summary>
        /// Get the exchange contract mapping of xep17 assets
        /// </summary>
        /// <returns></returns>
        public static ExchangePair[] GetAllExchangePair()
        {
            var iterator = (Iterator<KeyValue>)StorageFind(ExchangeMapKey);
            var result = new ExchangePair[0];
            while (iterator.Next())
            {
                var keyValue = iterator.Value;
                if (keyValue.Value != null)
                {
                    var exchangeContractHash = keyValue.Value;
                    var tokenA = keyValue.Key.Take(20);
                    var tokenB = keyValue.Key.Last(20);
                    var item = new ExchangePair()
                    {
                        TokenA = (UInt160)tokenA,
                        TokenB = (UInt160)tokenB,
                        ExchangePairHash = exchangeContractHash,
                    };
                    Append(result, item);
                }
            }
            return result;
        }




        /// <summary>
        /// Get a pair
        /// </summary>
        /// <param name="tokenA"></param>
        /// <param name="tokenB"></param>
        /// <returns></returns>
        private static byte[] GetPairKey(UInt160 tokenA, UInt160 tokenB)
        {
            return tokenA.ToUInteger() < tokenB.ToUInteger()
                ? ExchangeMapKey.Concat(tokenA).Concat(tokenB)
                : ExchangeMapKey.Concat(tokenB).Concat(tokenA);
        }
    }
}

