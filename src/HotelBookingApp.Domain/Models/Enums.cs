namespace HotelBookingApp.Domain.Models;

/// <summary>
/// Trạng thái phê duyệt của khách sạn.
/// </summary>
public enum HotelApprovalStatus
{
    Draft = 0,
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

/// <summary>
/// Trạng thái xác thực của doanh nghiệp.
/// </summary>
public enum BusinessVerificationStatus
{
    Pending,
    Approved,
    Rejected
}

/// <summary>
/// Trạng thái đơn đặt phòng.
/// Pending: Chờ xác nhận từ khách sạn.
/// Approved: Khách sạn đã duyệt, chờ khách thanh toán cọc.
/// Confirmed: Khách đã thanh toán cọc (nếu có yêu cầu).
/// Cancelled: Đã hủy (có lý do).
/// Completed: Đã hoàn thành (sau ngày trả phòng).
/// </summary>
public enum BookingStatus
{
    Pending,
    Approved,
    Confirmed,
    Cancelled,
    Completed
}

/// <summary>
/// Trạng thái thanh toán tiền cọc.
/// Unpaid: Chưa thanh toán cọc.
/// Paid: Đã thanh toán cọc.
/// Refunded: Đã hoàn cọc cho khách.
/// </summary>
public enum PaymentStatus
{
    Unpaid,
    Paid,
    Refunded
}

public enum PaymentMethod
{
    Cash,
    BankTransfer,
    CreditCard
}

/// <summary>
/// Trạng thái của một giao dịch thanh toán (qua cổng thanh toán).
/// </summary>
public enum PaymentTransactionStatus
{
    Pending,
    Success,
    Failed,
    Cancelled
}
