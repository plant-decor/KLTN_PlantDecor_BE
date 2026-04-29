namespace PlantDecor.BusinessLogicLayer.Libraries
{
    public static class EmailTemplateReader
    {
        public static string ConfirmationTemplate(string userName, string confirmUrl)
        {
            return $@"
        <html>
        <body style='font-family: Arial;'>
            <h2>Hello {userName},</h2>
            <p>Thank you for registering an account at <b>PlantDecor</b>.</p>
            <a href='https://www.plantdecor.io.vn/' style='text-decoration:none;'>
                <img src='https://res.cloudinary.com/dliirxsmo/image/upload/v1776617018/DSfep_s24rkv.jpg' alt='Logo' width='196'/>
            </a>
            <p>Please confirm your email by clicking the button below:</p>
            <p>
                <a href='{confirmUrl}' 
                   style='background-color: #007bff; color: white; padding: 10px 20px; 
                          text-decoration: none; border-radius: 5px;'>Confirm Email</a>
            </p>
            <p>If you did not sign up, please ignore this email.</p>
        </body>
        </html>";
        }

        public static string OtpEmailVerificationTemplate(string userName, string otpCode, DateTime expiresAt)
        {
            var expiresInMinutes = Math.Round((expiresAt - DateTime.UtcNow).TotalMinutes);

            return $@"
        <html>
        <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;'>
            <div style='max-width: 600px; margin: 0 auto; background-color: white;
                        border-radius: 10px; padding: 40px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                <div style='text-align: center; margin-bottom: 30px;'>
                    <h1 style='color: #2c3e50; font-size: 24px; margin-bottom: 10px;'>
                        PlantDecor
                    </h1>
                    <div style='width: 50px; height: 3px; background-color: #4CAF50;
                                margin: 10px auto;'></div>
                </div>

                <div style='text-align: center;'>
                    <h2 style='color: #34495e; font-size: 20px; margin-bottom: 10px;'>
                        Hello {userName},
                    </h2>
                        <a href='https://www.plantdecor.io.vn/' style='text-decoration:none;'>
                            <img src='https://res.cloudinary.com/dliirxsmo/image/upload/v1776617018/DSfep_s24rkv.jpg' alt='Logo' width='196'/>
                        </a>
                    <p style='color: #7f8c8d; font-size: 16px; margin-bottom: 30px;'>
                        Your email verification code is:
                    </p>

                    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                                border-radius: 15px; padding: 30px; margin: 30px 0;
                                box-shadow: 0 5px 15px rgba(102, 126, 234, 0.4);'>
                        <div style='color: white; font-size: 14px; margin-bottom: 10px;
                                    text-transform: uppercase; letter-spacing: 2px;'>
                            Email Verification Code
                        </div>
                        <div style='color: white; font-size: 48px; font-weight: bold;
                                    letter-spacing: 12px; font-family: monospace;
                                    text-shadow: 2px 2px 4px rgba(0,0,0,0.2);'>
                            {otpCode}
                        </div>
                    </div>

                    <div style='background-color: #fff3cd; border-left: 4px solid #ffc107;
                                padding: 15px; margin: 20px 0; text-align: left;'>
                        <p style='color: #856404; margin: 0; font-size: 14px;'>
                            <strong>⏱️ Note:</strong> This code will expire in <strong>{expiresInMinutes} minutes</strong>
                        </p>
                    </div>

                    <p style='color: #7f8c8d; font-size: 14px; margin-top: 30px;'>
                        If you did not request this code, please ignore this email.
                    </p>

                    <div style='margin-top: 40px; padding-top: 20px;
                                border-top: 1px solid #ecf0f1;'>
                        <p style='color: #95a5a6; font-size: 12px; margin: 0;'>
                            © 2026 PlantDecor. All rights reserved.
                        </p>
                    </div>
                </div>
            </div>
        </body>
        </html>";
        }

        public static string OtpPasswordResetTemplate(string userName, string otpCode, DateTime expiresAt)
        {
            var expiresInMinutes = Math.Round((expiresAt - DateTime.UtcNow).TotalMinutes);

            return $@"
        <html>
        <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;'>
            <div style='max-width: 600px; margin: 0 auto; background-color: white;
                        border-radius: 10px; padding: 40px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                <div style='text-align: center; margin-bottom: 30px;'>
                    <h1 style='color: #2c3e50; font-size: 24px; margin-bottom: 10px;'>
                        PlantDecor
                    </h1>
                    <div style='width: 50px; height: 3px; background-color: #e74c3c;
                                margin: 10px auto;'></div>
                </div>

                <div style='text-align: center;'>
                    <h2 style='color: #34495e; font-size: 20px; margin-bottom: 10px;'>
                        Hello {userName},
                    </h2>
                        <a href='https://www.plantdecor.io.vn/' style='text-decoration:none;'>
                            <img src='https://res.cloudinary.com/dliirxsmo/image/upload/v1776617018/DSfep_s24rkv.jpg' alt='Logo' width='196'/>
                        </a>
                    <p style='color: #7f8c8d; font-size: 16px; margin-bottom: 30px;'>
                        Your password reset code is:
                    </p>

                    <div style='background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
                                border-radius: 15px; padding: 30px; margin: 30px 0;
                                box-shadow: 0 5px 15px rgba(245, 87, 108, 0.4);'>
                        <div style='color: white; font-size: 14px; margin-bottom: 10px;
                                    text-transform: uppercase; letter-spacing: 2px;'>
                            Password Reset Code
                        </div>
                        <div style='color: white; font-size: 48px; font-weight: bold;
                                    letter-spacing: 12px; font-family: monospace;
                                    text-shadow: 2px 2px 4px rgba(0,0,0,0.2);'>
                            {otpCode}
                        </div>
                    </div>

                    <div style='background-color: #f8d7da; border-left: 4px solid #dc3545;
                                padding: 15px; margin: 20px 0; text-align: left;'>
                        <p style='color: #721c24; margin: 0; font-size: 14px;'>
                            <strong>⏱️ Note:</strong> This code will expire in <strong>{expiresInMinutes} minutes</strong>
                        </p>
                    </div>

                    <div style='background-color: #fff3cd; border-left: 4px solid #ffc107;
                                padding: 15px; margin: 20px 0; text-align: left;'>
                        <p style='color: #856404; margin: 0; font-size: 14px;'>
                            <strong>🔒 Security:</strong> If you did not request a password reset, please ignore this email and secure your account immediately.
                        </p>
                    </div>

                    <div style='margin-top: 40px; padding-top: 20px;
                                border-top: 1px solid #ecf0f1;'>
                        <p style='color: #95a5a6; font-size: 12px; margin: 0;'>
                            © 2026 PlantDecor. All rights reserved.
                        </p>
                    </div>
                </div>
            </div>
        </body>
        </html>";
        }
    }
}
