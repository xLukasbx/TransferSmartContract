using Stratis.SmartContracts;

namespace TransferSmartContract
{
    class Transfer : SmartContract
    {
        public Address Sender
        {
            get
            {
                return PersistentState.GetAddress("Sender");
            }
            private set
            {
                PersistentState.SetAddress("Sender", value);
            }
        }

        public Address OwnerContract
        {
            get
            {
                return PersistentState.GetAddress("OwnerContract");
            }
            private set
            {
                PersistentState.SetAddress("OwnerContract", value);
            }
        }

        public ulong Money
        {
            get
            {
                return PersistentState.GetUInt64("Money");
            }
            private set
            {
                PersistentState.SetUInt64("Money", value);
            }
        }

        public ISmartContractMapping<ulong> ReturnBalances
        {
            get
            {
                return PersistentState.GetUInt64Mapping("ReturnBalances");
            }
        }

        public Transfer(ISmartContractState smartContractState)
        : base(smartContractState)
        {
            OwnerContract = Message.Sender;
        }

        public void TransferCashToContract()
        {
            if (Money > 0)
            {
                ReturnBalances[Sender] = Money;
            }
            Sender = Message.Sender;
            Money += Message.Value;
        }
    }
}
