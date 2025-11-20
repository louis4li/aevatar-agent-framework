using Aevatar.Silo.Messages;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace Aevatar.Silo.Agents;

/// <summary>
/// Bank Account Agent with Event Sourcing
/// Demonstrates: Event sourcing pattern with Orleans
/// - Batch event commit for performance
/// - Pure functional state transitions
/// - Crash recovery through event replay
/// - Snapshot support
/// </summary>
public class BankAccountAgent // : GAgentBase<BankAccountState>
{
    private readonly ILogger<BankAccountAgent> _logger;

    public BankAccountAgent(ILogger<BankAccountAgent> logger)
    {
        _logger = logger;
    }

    // TODO: Implement with GAgentBase + EventSourcing when framework is integrated
    /*
    /// <summary>
    /// Create a new bank account
    /// </summary>
    public async Task CreateAccountAsync(string accountHolder, double initialBalance)
    {
        if (State.AccountId != null)
        {
            throw new InvalidOperationException("Account already exists");
        }

        if (initialBalance < 0)
        {
            throw new ArgumentException("Initial balance cannot be negative");
        }

        var evt = new AccountCreated
        {
            AccountHolder = accountHolder,
            InitialBalance = initialBalance,
            AccountId = AgentId.ToString(),
            CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        // Raise event (batched, not persisted yet)
        RaiseEvent(evt);

        // Commit all pending events to EventStore
        await ConfirmEventsAsync();

        _logger.LogInformation(
            "[BankAccount] Account created for {AccountHolder} with balance {Balance}",
            accountHolder,
            initialBalance
        );
    }

    /// <summary>
    /// Deposit money into account
    /// </summary>
    public async Task DepositAsync(double amount, string description)
    {
        if (State.AccountId == null)
        {
            throw new InvalidOperationException("Account not created");
        }

        if (amount <= 0)
        {
            throw new ArgumentException("Deposit amount must be positive");
        }

        var evt = new MoneyDeposited
        {
            Amount = amount,
            Description = description,
            DepositedAt = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        RaiseEvent(evt);
        await ConfirmEventsAsync();

        _logger.LogInformation(
            "[BankAccount] Deposited {Amount} - {Description}. New balance: {Balance}",
            amount,
            description,
            State.Balance
        );
    }

    /// <summary>
    /// Withdraw money from account
    /// </summary>
    public async Task WithdrawAsync(double amount, string description)
    {
        if (State.AccountId == null)
        {
            throw new InvalidOperationException("Account not created");
        }

        if (amount <= 0)
        {
            throw new ArgumentException("Withdrawal amount must be positive");
        }

        if (State.Balance < amount)
        {
            throw new InvalidOperationException($"Insufficient funds. Balance: {State.Balance}, Requested: {amount}");
        }

        var evt = new MoneyWithdrawn
        {
            Amount = amount,
            Description = description,
            WithdrawnAt = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        RaiseEvent(evt);
        await ConfirmEventsAsync();

        _logger.LogInformation(
            "[BankAccount] Withdrew {Amount} - {Description}. New balance: {Balance}",
            amount,
            description,
            State.Balance
        );
    }

    /// <summary>
    /// Batch multiple transactions for performance
    /// </summary>
    public async Task ProcessBatchTransactionsAsync(IEnumerable<(bool IsDeposit, double Amount, string Description)> transactions)
    {
        if (State.AccountId == null)
        {
            throw new InvalidOperationException("Account not created");
        }

        foreach (var (isDeposit, amount, description) in transactions)
        {
            if (amount <= 0)
            {
                throw new ArgumentException($"Transaction amount must be positive: {amount}");
            }

            if (isDeposit)
            {
                RaiseEvent(new MoneyDeposited
                {
                    Amount = amount,
                    Description = description,
                    DepositedAt = Timestamp.FromDateTime(DateTime.UtcNow)
                });
            }
            else
            {
                if (State.Balance < amount)
                {
                    throw new InvalidOperationException($"Insufficient funds for withdrawal: {amount}");
                }

                RaiseEvent(new MoneyWithdrawn
                {
                    Amount = amount,
                    Description = description,
                    WithdrawnAt = Timestamp.FromDateTime(DateTime.UtcNow)
                });
            }
        }

        // Batch commit all events at once (10-100x performance improvement)
        await ConfirmEventsAsync();

        _logger.LogInformation(
            "[BankAccount] Processed {Count} batch transactions. New balance: {Balance}",
            transactions.Count(),
            State.Balance
        );
    }

    /// <summary>
    /// Get current account balance
    /// </summary>
    public Task<double> GetBalanceAsync()
    {
        return Task.FromResult(State.Balance);
    }

    /// <summary>
    /// Get account information
    /// </summary>
    public Task<BankAccountState> GetAccountInfoAsync()
    {
        return Task.FromResult(State);
    }

    /// <summary>
    /// Pure functional state transition
    /// CRITICAL: Do NOT modify the input state - create a new copy
    /// </summary>
    protected override BankAccountState TransitionState(BankAccountState state, IMessage evt)
    {
        // Clone state to maintain immutability
        var newState = state.Clone();

        switch (evt)
        {
            case AccountCreated created:
                newState.AccountId = created.AccountId;
                newState.AccountHolder = created.AccountHolder;
                newState.Balance = created.InitialBalance;
                newState.CreatedAt = created.CreatedAt;
                break;

            case MoneyDeposited deposited:
                newState.Balance += deposited.Amount;
                newState.TransactionCount++;
                newState.LastTransactionTime = deposited.DepositedAt;
                break;

            case MoneyWithdrawn withdrawn:
                newState.Balance -= withdrawn.Amount;
                newState.TransactionCount++;
                newState.LastTransactionTime = withdrawn.WithdrawnAt;
                break;

            default:
                _logger.LogWarning("Unknown event type: {EventType}", evt.GetType().Name);
                break;
        }

        return newState;
    }

    /// <summary>
    /// Snapshot strategy: create snapshot every 10 events
    /// </summary>
    protected override ISnapshotStrategy SnapshotStrategy => new IntervalSnapshotStrategy(10);

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult($"Bank Account: {State.AccountHolder ?? "N/A"} (Balance: {State.Balance})");
    }

    /// <summary>
    /// OnActivateAsync: State is automatically restored from EventStore
    /// No need to manually load events - framework handles it
    /// </summary>
    public override async Task OnActivateAsync(CancellationToken ct = default)
    {
        await base.OnActivateAsync(ct);

        if (State.AccountId != null)
        {
            _logger.LogInformation(
                "[BankAccount] Restored from EventStore: {AccountHolder}, Balance: {Balance}, Transactions: {Count}",
                State.AccountHolder,
                State.Balance,
                State.TransactionCount
            );
        }
        else
        {
            _logger.LogInformation("[BankAccount] New account agent activated (no events yet)");
        }
    }
    */
}

