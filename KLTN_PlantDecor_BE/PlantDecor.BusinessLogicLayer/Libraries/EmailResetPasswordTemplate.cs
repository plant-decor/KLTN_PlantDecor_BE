namespace PlantDecor.BusinessLogicLayer.Libraries
{
    public static class EmailResetPasswordTemplate
    {
        public static string ResetConfirmationTemplate(string userName, string confirmUrl)
        {
            return $@"
        <html>
        <body style='font-family: Arial;'>
            <h2>Hello {userName},</h2>
            <a href='https://www.plantdecor.io.vn/' style='text-decoration:none;'>
                <img src='https://res.cloudinary.com/dliirxsmo/image/upload/v1776617018/DSfep_s24rkv.jpg' alt='Logo' width='196'/>
            </a>
            <p>You requested a password reset for your <b>PlantDecor</b> account.</p>
            <p>Please click the button below to reset your password:</p>
            <p>
                <a href='{confirmUrl}' 
                   style='background-color: #007bff; color: white; padding: 10px 20px; 
                          text-decoration: none; border-radius: 5px;'>Reset Password</a>
            </p>
            <p>If you did not request this, please ignore this email.</p>
        </body>
        </html>";
        }
    }
}
