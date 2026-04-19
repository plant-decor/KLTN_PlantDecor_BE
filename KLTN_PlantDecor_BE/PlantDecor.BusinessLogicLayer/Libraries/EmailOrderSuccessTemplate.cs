namespace PlantDecor.BusinessLogicLayer.Libraries
{
    public static class EmailOrderSuccessTemplate
    {
        public static string OrderSuccessTemplate(
        string userName,
        string orderId,
        string amount,
        string orderDate,
        string productName,
        string supportEmail = "thangnguyenduc2004@gmail.com")
        {
            return $@"
<html>
<body style='font-family: Arial, sans-serif; background-color:#f7f7f7; padding:20px;'>

    <div style='max-width:600px; margin:auto; background:white; padding:25px; border-radius:10px;
                box-shadow:0 2px 8px rgba(0,0,0,0.1);'>

        <div style='text-align:center;'>
            <a href='https://www.plantdecor.io.vn/' style='text-decoration:none;'>
                <img src='https://res.cloudinary.com/dliirxsmo/image/upload/v1776617018/DSfep_s24rkv.jpg'
                     alt='PlantDecor Logo' width='196' style='margin-bottom:15px;'/>
            </a>
            <h2 style='color:#28a745;'>Thanh toán thành công!</h2>
        </div>

        <p>Xin chào <b>{userName}</b>,</p>
        <p>Cảm ơn bạn đã thanh toán đơn hàng thông qua <b>VNPay</b>. Dưới đây là thông tin đơn hàng của bạn:</p>

        <table style='width:100%; border-collapse:collapse; margin-top:15px;'>
            <tr>
                <td style='padding:8px 0; color:#555;'>Mã đơn hàng:</td>
                <td style='text-align:right; font-weight:bold;'>{orderId}</td>
            </tr>
            <tr>
                <td style='padding:8px 0; color:#555;'>Sản phẩm:</td>
                <td style='text-align:right; font-weight:bold;'>{productName}</td>
            </tr>
            <tr>
                <td style='padding:8px 0; color:#555;'>Ngày thanh toán:</td>
                <td style='text-align:right; font-weight:bold;'>{orderDate}</td>
            </tr>
            <tr>
                <td style='padding:8px 0; color:#555;'>Tổng tiền:</td>
                <td style='text-align:right; font-weight:bold; color:#28a745;'>{amount}</td>
            </tr>
        </table>

        <p style='margin-top:25px; color:#555;'>
            Nếu bạn cần hỗ trợ, vui lòng liên hệ: 
            <b>{supportEmail}</b>.
        </p>

        <p style='color:#777; font-size:13px; margin-top:15px; text-align:center;'>
            Cảm ơn bạn đã tin tưởng PlantDecor!
        </p>

    </div>

</body>
</html>";
        }
    }
}
