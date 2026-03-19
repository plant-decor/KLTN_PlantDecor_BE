namespace PlantDecor.BusinessLogicLayer.Libraries
{
    public static class EmailTemplateReader
    {
        public static string ConfirmationTemplate(string userName, string confirmUrl)
        {
            return $@"
        <html>
        <body style='font-family: Arial;'>
            <h2>Xin chào {userName},</h2>
            <p>Cảm ơn bạn đã đăng ký tài khoản tại <b>PlantDecor</b>.</p>
            <p>Vui lòng xác nhận email của bạn bằng cách nhấn nút bên dưới:</p>
            <p>
                <a href='{confirmUrl}' 
                   style='background-color: #007bff; color: white; padding: 10px 20px; 
                          text-decoration: none; border-radius: 5px;'>Xác nhận Email</a>
            </p>
            <p>Nếu bạn không đăng ký, hãy bỏ qua email này.</p>
        </body>
        </html>";
        }

        public static string OtpTemplate(string userName, string otpCode, DateTime expiresAt)
        {
            // Format OTP with spaces (e.g., "1 4 7 2 5 2")
            var formattedOtp = string.Join(" ", otpCode.ToCharArray());
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
                        Xin chào {userName},
                    </h2>
                    <p style='color: #7f8c8d; font-size: 16px; margin-bottom: 30px;'>
                        Mã xác thực của bạn là:
                    </p>

                    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                                border-radius: 15px; padding: 30px; margin: 30px 0;
                                box-shadow: 0 5px 15px rgba(102, 126, 234, 0.4);'>
                        <div style='color: white; font-size: 14px; margin-bottom: 10px;
                                    text-transform: uppercase; letter-spacing: 2px;'>
                            Mã Đăng Nhập
                        </div>
                        <div style='color: white; font-size: 48px; font-weight: bold;
                                    letter-spacing: 12px; font-family: monospace;
                                    text-shadow: 2px 2px 4px rgba(0,0,0,0.2);'>
                            {formattedOtp}
                        </div>
                    </div>

                    <div style='background-color: #fff3cd; border-left: 4px solid #ffc107;
                                padding: 15px; margin: 20px 0; text-align: left;'>
                        <p style='color: #856404; margin: 0; font-size: 14px;'>
                            <strong>⏱️ Lưu ý:</strong> Mã này sẽ hết hạn sau <strong>{expiresInMinutes} phút</strong>
                        </p>
                    </div>

                    <p style='color: #7f8c8d; font-size: 14px; margin-top: 30px;'>
                        Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email này.
                    </p>

                    <div style='margin-top: 40px; padding-top: 20px;
                                border-top: 1px solid #ecf0f1;'>
                        <p style='color: #95a5a6; font-size: 12px; margin: 0;'>
                            © 2024 PlantDecor. All rights reserved.
                        </p>
                    </div>
                </div>
            </div>
        </body>
        </html>";
        }
    }
}
