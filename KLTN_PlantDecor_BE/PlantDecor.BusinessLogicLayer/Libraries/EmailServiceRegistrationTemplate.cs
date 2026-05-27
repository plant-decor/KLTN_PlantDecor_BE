namespace PlantDecor.BusinessLogicLayer.Libraries
{
    public static class EmailServiceRegistrationTemplate
    {
        private const string LogoUrl =
            "https://res.cloudinary.com/dliirxsmo/image/upload/v1776617018/DSfep_s24rkv.jpg";

        private const string SupportEmail = "thangnguyenduc2004@gmail.com";

        // ──────────────────────────────────────────────────────────────────────────
        // #1  Registration Created
        // ──────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Email sent to customer after successful service registration.
        /// </summary>
        public static string RegistrationCreatedTemplate(
            string userName,
            string registrationId,
            string packageName,
            string serviceDate,
            string nurseryName,
            string status)
        {
            return $@"
<html>
<body style='font-family: Arial, sans-serif; background-color:#f0f4f0; padding:20px;'>
  <div style='max-width:600px; margin:auto; background:white; padding:30px;
              border-radius:12px; box-shadow:0 2px 10px rgba(0,0,0,0.1);'>

    <div style='text-align:center;'>
      <a href='https://www.plantdecor.io.vn/' style='text-decoration:none;'>
        <img src='{LogoUrl}' alt='PlantDecor Logo' width='196' style='margin-bottom:15px;'/>
      </a>
      <h2 style='color:#2e7d32;'>🌿 Service Registration Successful!</h2>
    </div>

    <p>Hello <b>{userName}</b>,</p>
    <p>We have received your plant care service registration. The nursery will review and approve it shortly.</p>

    <table style='width:100%; border-collapse:collapse; margin-top:15px;'>
      <tr style='background:#f9f9f9;'>
        <td style='padding:10px; color:#555; border-bottom:1px solid #eee;'>Registration ID:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; border-bottom:1px solid #eee;'>#{registrationId}</td>
      </tr>
      <tr>
        <td style='padding:10px; color:#555; border-bottom:1px solid #eee;'>Service Package:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; border-bottom:1px solid #eee;'>{packageName}</td>
      </tr>
      <tr style='background:#f9f9f9;'>
        <td style='padding:10px; color:#555; border-bottom:1px solid #eee;'>Expected Start Date:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; border-bottom:1px solid #eee;'>{serviceDate}</td>
      </tr>
      <tr>
        <td style='padding:10px; color:#555; border-bottom:1px solid #eee;'>Assigned Nursery:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; border-bottom:1px solid #eee;'>{nurseryName}</td>
      </tr>
      <tr style='background:#f9f9f9;'>
        <td style='padding:10px; color:#555;'>Status:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; color:#f57c00;'>{status}</td>
      </tr>
    </table>

    <p style='margin-top:20px; color:#555;'>
      You can track the status of your registration in the PlantDecor app.
      If you need support, please contact: <b>{SupportEmail}</b>.
    </p>

    <p style='color:#777; font-size:13px; margin-top:15px; text-align:center;'>
      Thank you for choosing PlantDecor! 🌱
    </p>
  </div>
</body>
</html>";
        }

        // ──────────────────────────────────────────────────────────────────────────
        // #2  Manager Approves
        // ──────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Email sent to customer when registration is approved — prompts payment.
        /// </summary>
        public static string RegistrationApprovedTemplate(
            string userName,
            string registrationId,
            string packageName,
            string serviceDate,
            string amount,
            string nurseryName)
        {
            return $@"
<html>
<body style='font-family: Arial, sans-serif; background-color:#f0f4f0; padding:20px;'>
  <div style='max-width:600px; margin:auto; background:white; padding:30px;
              border-radius:12px; box-shadow:0 2px 10px rgba(0,0,0,0.1);'>

    <div style='text-align:center;'>
      <a href='https://www.plantdecor.io.vn/' style='text-decoration:none;'>
        <img src='{LogoUrl}' alt='PlantDecor Logo' width='196' style='margin-bottom:15px;'/>
      </a>
      <h2 style='color:#2e7d32;'>✅ Registration Approved!</h2>
    </div>

    <p>Hello <b>{userName}</b>,</p>
    <p>Great news! Your plant care service registration has been approved by <b>{nurseryName}</b>.
       Please proceed with the payment to activate the service.</p>

    <table style='width:100%; border-collapse:collapse; margin-top:15px;'>
      <tr style='background:#f9f9f9;'>
        <td style='padding:10px; color:#555; border-bottom:1px solid #eee;'>Registration ID:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; border-bottom:1px solid #eee;'>#{registrationId}</td>
      </tr>
      <tr>
        <td style='padding:10px; color:#555; border-bottom:1px solid #eee;'>Service Package:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; border-bottom:1px solid #eee;'>{packageName}</td>
      </tr>
      <tr style='background:#f9f9f9;'>
        <td style='padding:10px; color:#555; border-bottom:1px solid #eee;'>Expected Start Date:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; border-bottom:1px solid #eee;'>{serviceDate}</td>
      </tr>
      <tr>
        <td style='padding:10px; color:#555; border-bottom:1px solid #eee;'>Assigned Nursery:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; border-bottom:1px solid #eee;'>{nurseryName}</td>
      </tr>
      <tr style='background:#e8f5e9;'>
        <td style='padding:10px; color:#555;'>Amount to Pay:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; color:#2e7d32; font-size:16px;'>{amount}</td>
      </tr>
    </table>

    <div style='text-align:center; margin-top:25px;'>
      <a href='https://www.plantdecor.io.vn/' 
         style='background:#2e7d32; color:white; padding:12px 30px; border-radius:8px;
                text-decoration:none; font-weight:bold; font-size:15px;'>
        Pay Now
      </a>
    </div>

    <p style='margin-top:20px; color:#555;'>
      If you need support, please contact: <b>{SupportEmail}</b>.
    </p>

    <p style='color:#777; font-size:13px; margin-top:15px; text-align:center;'>
      Thank you for choosing PlantDecor! 🌱
    </p>
  </div>
</body>
</html>";
        }

        // ──────────────────────────────────────────────────────────────────────────
        // #3  Manager Rejects (final)
        // ──────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Email sent to customer when registration is fully rejected.
        /// </summary>
        public static string RegistrationRejectedTemplate(
            string userName,
            string registrationId,
            string packageName,
            string? rejectReason)
        {
            var reasonHtml = string.IsNullOrWhiteSpace(rejectReason)
                ? "<p style='color:#555;'>We currently do not have enough staff to fulfill your request. We apologize for the inconvenience.</p>"
                : $"<p style='color:#555;'>Reason for rejection: <b>{rejectReason}</b></p>";

            return $@"
<html>
<body style='font-family: Arial, sans-serif; background-color:#f5f5f5; padding:20px;'>
  <div style='max-width:600px; margin:auto; background:white; padding:30px;
              border-radius:12px; box-shadow:0 2px 10px rgba(0,0,0,0.1);'>

    <div style='text-align:center;'>
      <a href='https://www.plantdecor.io.vn/' style='text-decoration:none;'>
        <img src='{LogoUrl}' alt='PlantDecor Logo' width='196' style='margin-bottom:15px;'/>
      </a>
      <h2 style='color:#c62828;'>❌ Registration Not Accepted</h2>
    </div>

    <p>Hello <b>{userName}</b>,</p>
    <p>We regret to inform you that your plant care service registration was not accepted.</p>
    {reasonHtml}

    <table style='width:100%; border-collapse:collapse; margin-top:15px;'>
      <tr style='background:#f9f9f9;'>
        <td style='padding:10px; color:#555; border-bottom:1px solid #eee;'>Registration ID:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; border-bottom:1px solid #eee;'>#{registrationId}</td>
      </tr>
      <tr>
        <td style='padding:10px; color:#555;'>Service Package:</td>
        <td style='padding:10px; font-weight:bold; text-align:right;'>{packageName}</td>
      </tr>
    </table>

    <p style='margin-top:20px; color:#555;'>
      You can try creating a new registration with a different time or service package.
      If you need support, please contact: <b>{SupportEmail}</b>.
    </p>

    <div style='text-align:center; margin-top:25px;'>
      <a href='https://www.plantdecor.io.vn/'
         style='background:#2e7d32; color:white; padding:12px 30px; border-radius:8px;
                text-decoration:none; font-weight:bold; font-size:15px;'>
        Register Again
      </a>
    </div>

    <p style='color:#777; font-size:13px; margin-top:20px; text-align:center;'>
      We apologize for the inconvenience and hope to serve you in the future! 🌱
    </p>
  </div>
</body>
</html>";
        }

        // ──────────────────────────────────────────────────────────────────────────
        // #4  Payment Successful (Service order)
        // ──────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Email sent to customer when service invoice is paid successfully.
        /// </summary>
        public static string ServicePaymentSuccessTemplate(
            string userName,
            string registrationId,
            string packageName,
            string amount,
            string paymentDate,
            string serviceDate,
            string nurseryName)
        {
            return $@"
<html>
<body style='font-family: Arial, sans-serif; background-color:#f0f4f0; padding:20px;'>
  <div style='max-width:600px; margin:auto; background:white; padding:30px;
              border-radius:12px; box-shadow:0 2px 10px rgba(0,0,0,0.1);'>

    <div style='text-align:center;'>
      <a href='https://www.plantdecor.io.vn/' style='text-decoration:none;'>
        <img src='{LogoUrl}' alt='PlantDecor Logo' width='196' style='margin-bottom:15px;'/>
      </a>
      <h2 style='color:#2e7d32;'>💳 Payment Successful!</h2>
    </div>

    <p>Hello <b>{userName}</b>,</p>
    <p>Your payment for the plant care service has been confirmed successfully via <b>VNPay</b>.
       Your care schedule is being arranged and you will be notified soon.</p>

    <table style='width:100%; border-collapse:collapse; margin-top:15px;'>
      <tr style='background:#f9f9f9;'>
        <td style='padding:10px; color:#555; border-bottom:1px solid #eee;'>Registration ID:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; border-bottom:1px solid #eee;'>#{registrationId}</td>
      </tr>
      <tr>
        <td style='padding:10px; color:#555; border-bottom:1px solid #eee;'>Service Package:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; border-bottom:1px solid #eee;'>{packageName}</td>
      </tr>
      <tr style='background:#f9f9f9;'>
        <td style='padding:10px; color:#555; border-bottom:1px solid #eee;'>Assigned Nursery:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; border-bottom:1px solid #eee;'>{nurseryName}</td>
      </tr>
      <tr>
        <td style='padding:10px; color:#555; border-bottom:1px solid #eee;'>Expected Start Date:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; border-bottom:1px solid #eee;'>{serviceDate}</td>
      </tr>
      <tr style='background:#f9f9f9;'>
        <td style='padding:10px; color:#555; border-bottom:1px solid #eee;'>Payment Date:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; border-bottom:1px solid #eee;'>{paymentDate}</td>
      </tr>
      <tr style='background:#e8f5e9;'>
        <td style='padding:10px; color:#555;'>Amount Paid:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; color:#2e7d32; font-size:16px;'>{amount}</td>
      </tr>
    </table>

    <p style='margin-top:20px; color:#555;'>
      If you need support, please contact: <b>{SupportEmail}</b>.
    </p>

    <p style='color:#777; font-size:13px; margin-top:15px; text-align:center;'>
      Thank you for choosing PlantDecor! 🌱
    </p>
  </div>
</body>
</html>";
        }

        // ──────────────────────────────────────────────────────────────────────────
        // #5  Care Schedule Created + assign caretaker
        // ──────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Email sent to customer when the care schedule is generated.
        /// </summary>
        public static string ScheduleCreatedTemplate(
            string userName,
            string registrationId,
            string packageName,
            string serviceDate,
            string totalSessions,
            string nurseryName,
            string? caretakerName)
        {
            var caretakerHtml = string.IsNullOrWhiteSpace(caretakerName)
                ? @"<tr style='background:#fff3e0;'>
                     <td style='padding:10px; color:#555;'>Assigned Staff:</td>
                     <td style='padding:10px; font-weight:bold; text-align:right; color:#e65100;'>To be assigned</td>
                   </tr>"
                : $@"<tr style='background:#e8f5e9;'>
                      <td style='padding:10px; color:#555;'>Assigned Staff:</td>
                      <td style='padding:10px; font-weight:bold; text-align:right; color:#2e7d32;'>{caretakerName}</td>
                    </tr>";

            return $@"
<html>
<body style='font-family: Arial, sans-serif; background-color:#f0f4f0; padding:20px;'>
  <div style='max-width:600px; margin:auto; background:white; padding:30px;
              border-radius:12px; box-shadow:0 2px 10px rgba(0,0,0,0.1);'>

    <div style='text-align:center;'>
      <a href='https://www.plantdecor.io.vn/' style='text-decoration:none;'>
        <img src='{LogoUrl}' alt='PlantDecor Logo' width='196' style='margin-bottom:15px;'/>
      </a>
      <h2 style='color:#2e7d32;'>📅 Care Schedule Ready!</h2>
    </div>

    <p>Hello <b>{userName}</b>,</p>
    <p>The care schedule for your service registration has been set up. The PlantDecor team will provide the service according to the schedule.</p>

    <table style='width:100%; border-collapse:collapse; margin-top:15px;'>
      <tr style='background:#f9f9f9;'>
        <td style='padding:10px; color:#555; border-bottom:1px solid #eee;'>Registration ID:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; border-bottom:1px solid #eee;'>#{registrationId}</td>
      </tr>
      <tr>
        <td style='padding:10px; color:#555; border-bottom:1px solid #eee;'>Service Package:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; border-bottom:1px solid #eee;'>{packageName}</td>
      </tr>
      <tr style='background:#f9f9f9;'>
        <td style='padding:10px; color:#555; border-bottom:1px solid #eee;'>Assigned Nursery:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; border-bottom:1px solid #eee;'>{nurseryName}</td>
      </tr>
      <tr>
        <td style='padding:10px; color:#555; border-bottom:1px solid #eee;'>Start Date:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; border-bottom:1px solid #eee;'>{serviceDate}</td>
      </tr>
      <tr style='background:#f9f9f9;'>
        <td style='padding:10px; color:#555; border-bottom:1px solid #eee;'>Total Sessions:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; border-bottom:1px solid #eee;'>{totalSessions} sessions</td>
      </tr>
      {caretakerHtml}
    </table>

    <p style='margin-top:20px; color:#555;'>
      You can view the detailed care schedule in the PlantDecor app.
      If you need support, please contact: <b>{SupportEmail}</b>.
    </p>

    <p style='color:#777; font-size:13px; margin-top:15px; text-align:center;'>
      Thank you for choosing PlantDecor! 🌱
    </p>
  </div>
</body>
</html>";
        }

        // ──────────────────────────────────────────────────────────────────────────
        // #6  Caretaker Reassigned
        // ──────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Email sent to customer when a different caretaker is assigned to a specific session.
        /// </summary>
        public static string CaretakerReassignedTemplate(
            string userName,
            string registrationId,
            string packageName,
            string sessionDate,
            string? shiftName,
            string nurseryName,
            string? caretakerName)
        {
            var caretakerHtml = string.IsNullOrWhiteSpace(caretakerName)
                ? @"<tr style='background:#fff3e0;'>
                     <td style='padding:10px; color:#555;'>New Staff:</td>
                     <td style='padding:10px; font-weight:bold; text-align:right; color:#e65100;'>To be assigned</td>
                   </tr>"
                : $@"<tr style='background:#e8f5e9;'>
                      <td style='padding:10px; color:#555;'>New Staff:</td>
                      <td style='padding:10px; font-weight:bold; text-align:right; color:#2e7d32;'>{caretakerName}</td>
                    </tr>";

            var shiftHtml = string.IsNullOrWhiteSpace(shiftName)
                ? ""
                : $@"<tr style='background:#f9f9f9;'>
                      <td style='padding:10px; color:#555; border-bottom:1px solid #eee;'>Shift:</td>
                      <td style='padding:10px; font-weight:bold; text-align:right; border-bottom:1px solid #eee;'>{shiftName}</td>
                    </tr>";

            return $@"
<html>
<body style='font-family: Arial, sans-serif; background-color:#f0f4f0; padding:20px;'>
  <div style='max-width:600px; margin:auto; background:white; padding:30px;
              border-radius:12px; box-shadow:0 2px 10px rgba(0,0,0,0.1);'>

    <div style='text-align:center;'>
      <a href='https://www.plantdecor.io.vn/' style='text-decoration:none;'>
        <img src='{LogoUrl}' alt='PlantDecor Logo' width='196' style='margin-bottom:15px;'/>
      </a>
      <h2 style='color:#2e7d32;'>🔄 Staff Update for Your Care Session</h2>
    </div>

    <p>Hello <b>{userName}</b>,</p>
    <p>There has been a change in the staff assigned to your upcoming plant care session. The new assigned staff will be responsible for the session as scheduled.</p>

    <table style='width:100%; border-collapse:collapse; margin-top:15px;'>
      <tr style='background:#f9f9f9;'>
        <td style='padding:10px; color:#555; border-bottom:1px solid #eee;'>Registration ID:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; border-bottom:1px solid #eee;'>#{registrationId}</td>
      </tr>
      <tr>
        <td style='padding:10px; color:#555; border-bottom:1px solid #eee;'>Service Package:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; border-bottom:1px solid #eee;'>{packageName}</td>
      </tr>
      <tr style='background:#f9f9f9;'>
        <td style='padding:10px; color:#555; border-bottom:1px solid #eee;'>Assigned Nursery:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; border-bottom:1px solid #eee;'>{nurseryName}</td>
      </tr>
      <tr>
        <td style='padding:10px; color:#555; border-bottom:1px solid #eee;'>Session Date:</td>
        <td style='padding:10px; font-weight:bold; text-align:right; border-bottom:1px solid #eee;'>{sessionDate}</td>
      </tr>
      {shiftHtml}
      {caretakerHtml}
    </table>

    <p style='margin-top:20px; color:#555;'>
      You can view the detailed care schedule in the PlantDecor app.
      If you need support, please contact: <b>{SupportEmail}</b>.
    </p>

    <p style='color:#777; font-size:13px; margin-top:15px; text-align:center;'>
      Thank you for choosing PlantDecor! 🌱
    </p>
  </div>
</body>
</html>";
        }
    }
}
