using Stratis.SmartContracts;

namespace TransferSmartContract
{
    public class TransferManager : SmartContract
    {
        public TransferManager(ISmartContractState smartContractState) : base(smartContractState)
        {

        }

        public Address CreateNewTransfer()
        {
            var createResult = this.Create<Transfer>();

            return createResult.NewContractAddress;
        }
    }
}
