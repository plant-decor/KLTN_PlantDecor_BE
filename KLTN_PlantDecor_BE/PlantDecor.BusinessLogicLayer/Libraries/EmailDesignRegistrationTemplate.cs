namespace PlantDecor.BusinessLogicLayer.Libraries
{
    public static class EmailDesignRegistrationTemplate
    {
        private const string LogoUrl = "https://res.cloudinary.com/dliirxsmo/image/upload/v1776617018/DSfep_s24rkv.jpg";
        private const string SupportEmail = "thangnguyenduc2004@gmail.com";

        public static string RegistrationCreatedTemplate(
            string userName,
            string registrationId,
            string tierName,
            string nurseryName,
            string totalAmount)
        {
            return $@"
<html>
<body style='font-family: Arial, sans-serif; background-color:#f0f4f0; padding:20px;'>
  <div style='max-width:600px; margin:auto; background:white; padding:30px; border-radius:12px; box-shadow:0 2px 10px rgba(0,0,0,0.1);'>
    <div style='text-align:center;'>
      <a href='https://www.plantdecor.io.vn/' style='text-decoration:none;'>
        <img src='{LogoUrl}' alt='PlantDecor Logo' width='196' style='margin-bottom:15px;'/>
      </a>
      <h2 style='color:#2e7d32;'>Design Registration Received</h2>
    </div>

    <p>Hello <b>{userName}</b>,</p>
    <p>Your design registration has been created successfully and is waiting for nursery approval.</p>

    <table style='width:100%; border-collapse:collapse; margin-top:15px;'>
      <tr style='background:#f9f9f9;'><td style='padding:10px; color:#555;'>Registration ID:</td><td style='padding:10px; font-weight:bold; text-align:right;'>#{registrationId}</td></tr>
      <tr><td style='padding:10px; color:#555;'>Package:</td><td style='padding:10px; font-weight:bold; text-align:right;'>{tierName}</td></tr>
      <tr style='background:#f9f9f9;'><td style='padding:10px; color:#555;'>Assigned Nursery:</td><td style='padding:10px; font-weight:bold; text-align:right;'>{nurseryName}</td></tr>
      <tr><td style='padding:10px; color:#555;'>Estimated Total:</td><td style='padding:10px; font-weight:bold; text-align:right; color:#2e7d32;'>{totalAmount}</td></tr>
    </table>

    <p style='margin-top:20px; color:#555;'>Need support? Contact <b>{SupportEmail}</b>.</p>
  </div>
</body>
</html>";
        }

        public static string RegistrationApprovedTemplate(
            string userName,
            string registrationId,
            string tierName,
            string nurseryName,
            string depositAmount,
            string orderHistoryUrl)
        {
            return $@"
<html>
<body style='font-family: Arial, sans-serif; background-color:#f0f4f0; padding:20px;'>
  <div style='max-width:600px; margin:auto; background:white; padding:30px; border-radius:12px; box-shadow:0 2px 10px rgba(0,0,0,0.1);'>
    <div style='text-align:center;'>
      <a href='https://www.plantdecor.io.vn/' style='text-decoration:none;'>
        <img src='{LogoUrl}' alt='PlantDecor Logo' width='196' style='margin-bottom:15px;'/>
      </a>
      <h2 style='color:#2e7d32;'>Design Registration Approved</h2>
    </div>

    <p>Hello <b>{userName}</b>,</p>
    <p>Your design registration has been approved. Please complete the deposit payment to start the process.</p>

    <table style='width:100%; border-collapse:collapse; margin-top:15px;'>
      <tr style='background:#f9f9f9;'><td style='padding:10px; color:#555;'>Registration ID:</td><td style='padding:10px; font-weight:bold; text-align:right;'>#{registrationId}</td></tr>
      <tr><td style='padding:10px; color:#555;'>Package:</td><td style='padding:10px; font-weight:bold; text-align:right;'>{tierName}</td></tr>
      <tr style='background:#f9f9f9;'><td style='padding:10px; color:#555;'>Nursery:</td><td style='padding:10px; font-weight:bold; text-align:right;'>{nurseryName}</td></tr>
      <tr><td style='padding:10px; color:#555;'>Deposit Amount:</td><td style='padding:10px; font-weight:bold; text-align:right; color:#2e7d32;'>{depositAmount}</td></tr>
    </table>

    <div style='text-align:center; margin-top:25px;'>
      <a href='{orderHistoryUrl}' style='background:#2e7d32; color:white; padding:12px 30px; border-radius:8px; text-decoration:none; font-weight:bold;'>View Order and Pay</a>
    </div>
  </div>
</body>
</html>";
        }

        public static string RegistrationRejectedTemplate(
            string userName,
            string registrationId,
            string tierName,
            string? rejectReason)
        {
            var reason = string.IsNullOrWhiteSpace(rejectReason)
                ? "No detailed reason was provided."
                : rejectReason;

            return $@"
<html>
<body style='font-family: Arial, sans-serif; background-color:#f5f5f5; padding:20px;'>
  <div style='max-width:600px; margin:auto; background:white; padding:30px; border-radius:12px; box-shadow:0 2px 10px rgba(0,0,0,0.1);'>
    <div style='text-align:center;'>
      <a href='https://www.plantdecor.io.vn/' style='text-decoration:none;'>
        <img src='{LogoUrl}' alt='PlantDecor Logo' width='196' style='margin-bottom:15px;'/>
      </a>
      <h2 style='color:#c62828;'>Design Registration Rejected</h2>
    </div>

    <p>Hello <b>{userName}</b>,</p>
    <p>Your design registration could not be accepted.</p>
    <p><b>Reason:</b> {reason}</p>

    <table style='width:100%; border-collapse:collapse; margin-top:15px;'>
      <tr style='background:#f9f9f9;'><td style='padding:10px; color:#555;'>Registration ID:</td><td style='padding:10px; font-weight:bold; text-align:right;'>#{registrationId}</td></tr>
      <tr><td style='padding:10px; color:#555;'>Package:</td><td style='padding:10px; font-weight:bold; text-align:right;'>{tierName}</td></tr>
    </table>

    <p style='margin-top:20px; color:#555;'>Need support? Contact <b>{SupportEmail}</b>.</p>
  </div>
</body>
</html>";
        }

        public static string CaretakerAssignedTemplate(
            string userName,
            string registrationId,
            string? caretakerName)
        {
            var displayCaretaker = string.IsNullOrWhiteSpace(caretakerName) ? "Assigned staff" : caretakerName;

            return $@"
<html>
<body style='font-family: Arial, sans-serif; background-color:#f0f4f0; padding:20px;'>
  <div style='max-width:600px; margin:auto; background:white; padding:30px; border-radius:12px; box-shadow:0 2px 10px rgba(0,0,0,0.1);'>
    <div style='text-align:center;'>
      <a href='https://www.plantdecor.io.vn/' style='text-decoration:none;'>
        <img src='{LogoUrl}' alt='PlantDecor Logo' width='196' style='margin-bottom:15px;'/>
      </a>
      <h2 style='color:#2e7d32;'>Design Staff Assigned</h2>
    </div>

    <p>Hello <b>{userName}</b>,</p>
    <p>Your design registration has been assigned to <b>{displayCaretaker}</b>.</p>
    <p>Registration ID: <b>#{registrationId}</b></p>
  </div>
</body>
</html>";
        }

        public static string RegistrationCancelledTemplate(
            string userName,
            string registrationId,
            string? cancelReason)
        {
            var reason = string.IsNullOrWhiteSpace(cancelReason)
                ? "No detailed reason was provided."
                : cancelReason;

            return $@"
<html>
<body style='font-family: Arial, sans-serif; background-color:#f5f5f5; padding:20px;'>
  <div style='max-width:600px; margin:auto; background:white; padding:30px; border-radius:12px; box-shadow:0 2px 10px rgba(0,0,0,0.1);'>
    <div style='text-align:center;'>
      <a href='https://www.plantdecor.io.vn/' style='text-decoration:none;'>
        <img src='{LogoUrl}' alt='PlantDecor Logo' width='196' style='margin-bottom:15px;'/>
      </a>
      <h2 style='color:#c62828;'>Design Registration Cancelled</h2>
    </div>

    <p>Hello <b>{userName}</b>,</p>
    <p>Your design registration has been cancelled.</p>
    <p>Registration ID: <b>#{registrationId}</b></p>
    <p><b>Reason:</b> {reason}</p>

    <p style='margin-top:20px; color:#555;'>Need support? Contact <b>{SupportEmail}</b>.</p>
  </div>
</body>
</html>";
        }
    }
}
