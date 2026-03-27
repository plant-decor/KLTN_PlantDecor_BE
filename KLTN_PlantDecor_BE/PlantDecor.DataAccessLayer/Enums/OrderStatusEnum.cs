namespace PlantDecor.DataAccessLayer.Enums
{
    public enum OrderStatusEnum
    {
        Pending = 0,
        DepositPaid = 1,
        Paid = 2,
        Assigned = 3,
        Shipping = 4,
        Delivered = 5,
        RemainingPaymentPending = 6,
        Completed = 7,
        Cancelled = 8,
        Failed = 9,
        RefundRequested = 10,
        Refunded = 11,
        Rejected = 12,
        PendingConfirmation = 13
    }
}
