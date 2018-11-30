using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Stratis.SmartContracts;
using System.Collections.Generic;

namespace TransferSmartContract.Test
{
    [TestClass]
    public class TransferTest
    {
        private static readonly Address CoinbaseAddress = (Address)"Sj2p6ZRHdLvywyi43HYoE4bu2TF1nvavjR";
        private static readonly Address ContractOwnerAddress = (Address)"STpNXhJY6yrX4oh5LUeFbcBGxGCVxfV1ac";
        private static readonly Address ContractAddress = (Address)"SkNHV22dsvWdVb6Mi8uSGRbA8q9YSpezbw";
        private static readonly Address SenderAddress = (Address)"STCutNQ1haZT472aYnuBYcqbyH7QiDMNox";

        private IList<Address> SampleAddresses = new List<Address> {
            new Address("SeMvVcDKTLBrxVua5GXmdF8qBYTbJZt4NJ"),
            new Address("Sipqve53hyjzTo2oU7PUozpT1XcmATnkTn"),
            new Address("SXTtiQq2S4LrjWj2QMnpFfRJuzHGkfxuCE"),
            new Address("STCutNQ1haZT472aYnuBYcqbyH7QiDMNox"),
            new Address("ScBovGKDLrSyjRqJVGjXWLrdpTufWKFzne")
        };

        private const ulong ContractDeployBlockNumber = 1;
        private const ulong GasLimit = 10000;
        private const uint MinimalValue = 5;

        private Dictionary<Address, ulong> BlockchainBalances;

        private TestSmartContractState SmartContractState;

        [TestInitialize]
        public void Initialize()
        {
            BlockchainBalances = new Dictionary<Address, ulong>();
            var block = new TestBlock
            {
                Coinbase = CoinbaseAddress,
                Number = ContractDeployBlockNumber
            };
            var message = new TestMessage
            {
                ContractAddress = ContractAddress,
                GasLimit = (Gas)GasLimit,
                Sender = ContractOwnerAddress,
                Value = 0u
            };
            var getContractBalance = new Func<ulong>(() => BlockchainBalances[ContractAddress]);
            var persistentState = new TestPersistentState();

            var internalTransactionExecutor = new TestInternalTransactionExecutor(BlockchainBalances, ContractAddress, SampleAddresses);
            var gasMeter = new TestGasMeter((Gas)GasLimit);
            var hashHelper = new TestInternalHashHelper();

            this.SmartContractState = new TestSmartContractState(
                block,
                message,
                persistentState,
                gasMeter,
                internalTransactionExecutor,
                getContractBalance,
                hashHelper
            );
        }

        [TestMethod]
        public void TestConstruction()
        {
            var transfer = new Transfer(SmartContractState, 5);

            Assert.AreEqual(ContractOwnerAddress, SmartContractState.PersistentState.GetAddress("OwnerContract"));
            Assert.AreEqual(MinimalValue, SmartContractState.PersistentState.GetUInt32("MinimalValue"));
        }

        [TestMethod]
        public void TestTransfer()
        {
            var transfer = new Transfer(SmartContractState, 5);

            Assert.AreEqual(0uL, SmartContractState.PersistentState.GetUInt64("Money"));

            ((TestMessage)SmartContractState.Message).Value = 5;
            transfer.TransferMoneyToContract();
            Assert.AreEqual(5uL, SmartContractState.PersistentState.GetUInt64("Money"));

            ((TestMessage)SmartContractState.Message).Value = 4;
            Assert.ThrowsException<SmartContractAssertException>(() => transfer.TransferMoneyToContract());

            ((TestMessage)SmartContractState.Message).Value = 7;
            transfer.TransferMoneyToContract();
            Assert.AreEqual(12uL, SmartContractState.PersistentState.GetUInt64("Money"));
        }

        [TestMethod]
        public void TestScenario_ContractAddressSenderAddress_VerifyBalanaces()
        {
            var transfer = new Transfer(SmartContractState, 5);

            BlockchainBalances[ContractAddress] = 0;
            BlockchainBalances[SenderAddress] = 100;

            ulong currentSimulatedBlockNumber = ContractDeployBlockNumber;

            currentSimulatedBlockNumber++;
            SetBlock(currentSimulatedBlockNumber);
            MockContractMethodCall(sender: SenderAddress, value: 5u);
            transfer.TransferMoneyToContract();

            currentSimulatedBlockNumber++;
            SetBlock(currentSimulatedBlockNumber);
            MockContractMethodCall(sender: SenderAddress, value: 10u);
            transfer.TransferMoneyToContract();

            var expectedBlockchainBalances = new Dictionary<Address, ulong>
            {
                { ContractAddress, 15 },
                { SenderAddress, 85 }
            };

            var expectedReturnBalances = new Dictionary<Address, ulong> {
                { SenderAddress, 5 }
            };

            foreach (var k in expectedBlockchainBalances.Keys)
            {
                Assert.IsTrue(BlockchainBalances[k] == expectedBlockchainBalances[k]);
            }

            foreach (var k in expectedReturnBalances.Keys)
            {
                Assert.IsTrue(transfer.ReturnBalances[k] == expectedReturnBalances[k]);
            }
        }

        private void SetBlock(ulong blockNumber)
        {
            Assert.IsTrue(SmartContractState.Block.Number <= blockNumber);
            ((TestBlock)SmartContractState.Block).Number = blockNumber;
        }

        private void MockContractMethodCall(Address sender, uint value)
        {
            Assert.IsTrue(BlockchainBalances.ContainsKey(sender));
            Assert.IsTrue(BlockchainBalances.ContainsKey(ContractAddress));
            Assert.IsTrue(BlockchainBalances[sender] >= value);

            ((TestMessage)SmartContractState.Message).Sender = sender;
            ((TestMessage)SmartContractState.Message).Value = value;

            BlockchainBalances[sender] -= value;
            BlockchainBalances[ContractAddress] += value;
        }
    }
}
