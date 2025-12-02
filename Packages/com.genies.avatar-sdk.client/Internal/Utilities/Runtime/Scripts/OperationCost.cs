namespace Genies.Utilities
{
    /// <summary>
    /// Can be used to enhance the precision of the <see cref="OperationQueue"/> algorithm. Values are relative and you should
    /// specify them as time cost estimations. <see cref="Unknown"/> values will act as if the operation had 0 cost (no time
    /// estimations will be performed). The rest of the values are relative to each project. For example, all operations enqueued
    /// with a high cost will count towards the average time of high cost operations. That average time will be used to estimate
    /// an operation cost only when enqueueing other high cost operations. Basically we compute separate time averages for the
    /// different costs except for unknown.
    /// </summary>
    public enum OperationCost
    {
        Unknown = 0,
        Low = 1,
        Medium = 2,
        High = 3,
    }
}
