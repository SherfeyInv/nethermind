using System.Collections.Generic;
using System.Threading.Tasks;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.DataMarketplace.Core.Domain;
using Nethermind.DataMarketplace.Core.Services;

namespace Nethermind.DataMarketplace.Consumers.Services
{
    public class ConsumerNotifier : IConsumerNotifier
    {
        private readonly INdmNotifier _notifier;

        public ConsumerNotifier(INdmNotifier notifier)
        {
            _notifier = notifier;
        }

        public Task SendDataRequestResultAsync(Keccak depositId, DataRequestResult result)
            => _notifier.NotifyAsync(new Notification("data_request_result",
                new
                {
                    depositId,
                    result = result.ToString()
                }));

        public Task SendDepositConfirmationsStatusAsync(Keccak depositId, string dataAssetName, uint confirmations,
            uint requiredConfirmations, uint confirmationTimestamp, bool confirmed)
            => _notifier.NotifyAsync(new Notification("deposit_confirmations",
                new
                {
                    depositId,
                    dataAssetName,
                    confirmations,
                    requiredConfirmations,
                    confirmationTimestamp,
                    confirmed
                }));

        public Task SendDataInvalidAsync(Keccak depositId, InvalidDataReason reason)
            => _notifier.NotifyAsync(new Notification("data_invalid",
                new
                {
                    depositId,
                    reason = reason.ToString()
                }));

        public Task SendSessionStartedAsync(Keccak depositId, Keccak sessionId)
            => _notifier.NotifyAsync(new Notification("session_started",
                new
                {
                    depositId,
                    sessionId
                }));

        public Task SendSessionFinishedAsync(Keccak depositId, Keccak sessionId)
            => _notifier.NotifyAsync(new Notification("session_finished",
                new
                {
                    depositId,
                    sessionId,
                }));

        public Task SendConsumerAccountLockedAsync(Address address)
            => _notifier.NotifyAsync(new Notification("consumer_account_locked",
                new
                {
                    address
                }));

        public Task SendConsumerAddressChangedAsync(Address newAddress, Address previousAddress)
            => _notifier.NotifyAsync(new Notification("consumer_address_changed",
                new
                {
                    newAddress,
                    previousAddress
                }));

        public Task SendProviderAddressChangedAsync(Address newAddress, Address previousAddress)
            => _notifier.NotifyAsync(new Notification("provider_address_changed",
                new
                {
                    newAddress,
                    previousAddress
                }));

        public Task SendDataAssetStateChangedAsync(Keccak id, string name, DataAssetState state)
            => _notifier.NotifyAsync(new Notification("data_asset_state_changed",
                new
                {
                    id,
                    name,
                    state = state.ToString()
                }));

        public Task SendDataAssetRemovedAsync(Keccak id, string name)
            => _notifier.NotifyAsync(new Notification("data_asset_removed",
                new
                {
                    id,
                    name
                }));

        public Task SendDataAvailabilityChangedAsync(Keccak depositId, Keccak sessionId, DataAvailability availability)
            => _notifier.NotifyAsync(new Notification("data_availability_changed",
                new
                {
                    depositId,
                    sessionId,
                    availability = availability.ToString()
                }));

        public Task SendDataStreamEnabledAsync(Keccak depositId, Keccak sessionId)
            => _notifier.NotifyAsync(new Notification("data_stream_enabled",
                new
                {
                    depositId,
                    sessionId
                }));

        public Task SendDataStreamDisabledAsync(Keccak depositId, Keccak sessionId)
            => _notifier.NotifyAsync(new Notification("data_stream_disabled",
                new
                {
                    depositId,
                    sessionId
                }));

        public Task SendDepositApprovalConfirmedAsync(Keccak dataAssetId, string dataAssetName)
            => _notifier.NotifyAsync(new Notification("deposit_approval_confirmed",
                new
                {
                    dataAssetId,
                    dataAssetName
                }));

        public Task SendDepositApprovalRejectedAsync(Keccak dataAssetId, string dataAssetName)
            => _notifier.NotifyAsync(new Notification("deposit_approval_rejected",
                new
                {
                    dataAssetId,
                    dataAssetName
                }));

        public Task SendClaimedEarlyRefundAsync(Keccak depositId, string dataAssetName, Keccak transactionHash)
            => _notifier.NotifyAsync(new Notification("claimed_early_refund",
                new
                {
                    depositId,
                    dataAssetName,
                    transactionHash
                }));
        
        public Task SendClaimedRefundAsync(Keccak depositId, string dataAssetName, Keccak transactionHash)
            => _notifier.NotifyAsync(new Notification("claimed_refund",
                new
                {
                    depositId,
                    dataAssetName,
                    transactionHash
                }));

        public Task SendBlockProcessedAsync(long blockNumber)
            => _notifier.NotifyAsync(new Notification("block_processed",
                new
                {
                    blockNumber
                }));
    }
}