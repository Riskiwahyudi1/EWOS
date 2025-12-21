using EWOS_MVC.Models;
using EWOS_MVC.Services;

using MimeKit;

// ----------------------------
//cheklist kemungkinan bug cek sebelum deploy!!!
// ------------------------
//1. duplikat cc email ??
//2. email penerima null ?? 

public class EmailService
{
    private readonly EmailSettings _emailSettings; // from appsetting.json
    private readonly AdUserService _adUserService; // get user from AD
    private readonly EmailContextHelper _emailContextHelper; // get email engineer and fabrication team from DB

    public static class EmailConstants
    {
        public const string BaseUrl = "https://localhost:7257";

        public const string AutomatedEmailNotice =
            "<i style='color:gray;'>This is an automated email. Please do not reply to this email.</i><br><br>";
    }


    public EmailService(EmailSettings emailSettings, EmailContextHelper emailContextHelper, AdUserService adUserService)
    {
        _emailSettings = emailSettings; 
        _emailContextHelper = emailContextHelper;
        _adUserService = adUserService;
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body, List<string>? ccEmails = null)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
        email.To.Add(MailboxAddress.Parse(toEmail));

        if (ccEmails != null)
        {
            foreach (var cc in ccEmails)
                email.Cc.Add(MailboxAddress.Parse(cc));
        }

        email.Subject = subject;
        email.Body = new BodyBuilder { HtmlBody = body }.ToMessageBody();

        using var smtp = new MailKit.Net.Smtp.SmtpClient();
        await smtp.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, MailKit.Security.SecureSocketOptions.Auto);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }

    // ----------------------------
    // Method untuk New Request
    // ----------------------------

    public async Task SendNewRequestEmail(ItemRequestModel request, bool isRo, int quantity, DateTime crd, string description)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (request.Users == null) throw new ArgumentNullException(nameof(request.Users));

        var adProperties = _adUserService.GetAllAdProperties(request.Users.UserName);

        // Ambil email manager jika ada
        string? managerEmail = null;
        if (adProperties.ContainsKey("managerEmail"))
        {
            var email = adProperties["managerEmail"] as string;
            if (!string.IsNullOrEmpty(email) && !email.StartsWith("(gagal") && email != "(tidak ada email)")
            {
                managerEmail = email;
            }
        }
        // ========================
        // Ambil data dari helper
        // ========================
        List<string> fabTeamEmails = await _emailContextHelper.GetFabTeamEmailsAsync();
        UserModel? fabEngineer = await _emailContextHelper.GetEngineerAsync();
        MachineCategoriesModel? mcCategory = await _emailContextHelper.GetMachineCategoriesAsync(request.MachineCategoryId);

        // ------------------------
        // File HTML (NA jika kosong)
        // ------------------------
        string quotationHtml = string.IsNullOrEmpty(request.QuantationPath)
            ? "<i style='color:gray;'>NA</i>"
            : $"<a href='{EmailConstants.BaseUrl}{request.QuantationPath}' target='_blank'>View</a>";

        string drawingHtml = string.IsNullOrEmpty(request.DrawingPath)
            ? "<i style='color:gray;'>NA</i>"
            : $"<a href='{EmailConstants.BaseUrl}{request.DrawingPath}' target='_blank'>View</a>";

        string designHtml = string.IsNullOrEmpty(request.DesignPath)
            ? "<i style='color:gray;'>NA</i>"
            : $"<a href='{EmailConstants.BaseUrl}{request.DesignPath}' target='_blank'>View</a>";

        string fabCategory = mcCategory?.CategoryName ?? "Unknown";

        var reqType = "";

        if(isRo == true)
        {
            reqType = "repeat order";
        }
        else
        {
            reqType = "request";
        }
        string designSection = "";

        if (!isRo) // Repeat Order = FALSE
        {
            designSection = $@"
                <tr style='background-color:#f2f2f2;'>
                    <td><b>Purchase Id</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'>
                        {(request.SAPID.HasValue ? request.SAPID.Value.ToString() : "NA")}
                    </td>
                </tr>
                <tr>
                    <td><b>External Price (USD)</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'>
                        {(request.ExternalFabCost.HasValue ? $"${request.ExternalFabCost.Value}" : "NA")}
                    </td>
                </tr>
                <tr style='background-color:#f2f2f2;'>
                    <td><b>Design</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'>{designHtml}</td>
                </tr>
                <tr>
                    <td><b>Quotation</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'>{quotationHtml}</td>
                </tr>
                <tr style='background-color:#f2f2f2;'>
                    <td><b>Drawing</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'>{drawingHtml}</td>
                </tr>
                <tr>
                    <td><b>Status</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'><b>Waiting Approval</b></td>
                </tr>
                
                
            ";
                }



        // ------------------------
        // 1. Body email USER
        // ------------------------
        string userBody = $@"
            <h3>Dear {request.Users.Name},</h3>
            Thank you for your {reqType} on Inhouse Fabrication. Here are the details:<br><br>

            <table border='1' cellpadding='5' cellspacing='0'
                   style='border-collapse: collapse; width:70%; table-layout: fixed;'>

                <colgroup>
                    <col style='width: 20%;'>
                    <col style='width: 50%;'>
                </colgroup>

                <tr style='background-color:#f2f2f2;'>
                    <td><b>ID Request</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'>{request.Id}</td>
                </tr>
                <tr>
                    <td><b>Part Name</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'>{request.PartName}</td>
                </tr>
                <tr style='background-color:#f2f2f2;'>
                    <td><b>Quantity</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'>
                        {quantity} {request.Unit}
                    </td>
                </tr>
                <tr>
                    <td><b>Request Category</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'>{fabCategory}</td>
                </tr>

                {designSection}
                
                <tr style='background-color:#f2f2f2;'>
                    <td><b>Customer Request Date</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'>
                        {crd:dd-MM-yyyy}
                    </td>
                </tr>
                <tr>
                    <td><b>Description</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'>
                        {description}
                    </td>
                </tr>

            </table> <br><br>

            Your request status is <b>waiting for response</b>.<br>
            Please monitor your request at <a href='{EmailConstants.BaseUrl}'>Inhouse Fabrication System</a>.<br><br>

            Best Regards,<br>
            {_emailSettings.SenderName}<br><br>
            {EmailConstants.AutomatedEmailNotice}";


        // ------------------------
        // 2. Body email ENGINEER
        // ------------------------

        string engineerBody = $@"
            <h3>Dear {fabEngineer?.Name ?? "Engineer"},</h3>
            New {reqType} fabrication  from <b>{request.Users.Name}</b> requires your approval:<br><br>

           <table border='1' cellpadding='5' cellspacing='0'
                  style='border-collapse: collapse; width:70%; table-layout: fixed;'>

                <colgroup>
                    <col style='width: 20%;'>
                    <col style='width: 50%;'>
                </colgroup>

                <tr style='background-color:#f2f2f2;'>
                    <td><b>ID Request</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'>{request.Id}</td>
                </tr>
                <tr>
                    <td><b>Part Name</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'>{request.PartName}</td>
                </tr>
                <tr style='background-color:#f2f2f2;'>
                    <td><b>Quantity</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'>
                        {quantity} {request.Unit}
                    </td>
                </tr>
                <tr>
                    <td><b>Request Category</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'>{fabCategory}</td>
                </tr>

                {designSection}

                <tr style='background-color:#f2f2f2;'>
                    <td><b>Customer Request Date</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'>
                        {crd:dd-MM-yyyy}
                    </td>
                </tr>
                <tr>
                    <td><b>Description</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'>
                        {description}
                    </td>
                </tr>

            </table> <br><br>
            Please review and take action via  
            <a href='{EmailConstants.BaseUrl}'>Inhouse Fabrication System</a><br><br>


            Best Regards,<br>
            {_emailSettings.SenderName}  <br><br>
            {EmailConstants.AutomatedEmailNotice} ";


        // ------------------------
        // 3. Kirim email ke REQUESTOR
        // ------------------------
        var userCC = fabTeamEmails.Distinct(StringComparer.OrdinalIgnoreCase).ToList(); 

        // Tambahkan email manager ke userCC jika ada dan belum ada di list
        //if (!string.IsNullOrEmpty(managerEmail) &&
        //    !userCC.Any(e => string.Equals(e, managerEmail, StringComparison.OrdinalIgnoreCase)))
        //{
        //    userCC.Add(managerEmail);
        //}

        await SendEmailAsync(
            request.Users.Email,
            "New Request Notification for Inhouse Fabrication",
            userBody,
            userCC // isinya role Admin Fabrication dan manager dari requestor
        );

        // ------------------------
        // 4. Kirim email ke ENGINEER
        // ------------------------
        if (fabEngineer != null && !string.IsNullOrEmpty(fabEngineer.Email))
        {
            var engineerCC = string.IsNullOrEmpty(request.Users.Email)
                ? null
                : new List<string> { request.Users.Email };

            await SendEmailAsync(
                fabEngineer.Email,
                "New Request Fabrication – Approval Needed",
                engineerBody,
                null
            );
        }

    }

    // ----------------------------
    // Method untuk Konfirmasi Done
    // ----------------------------
    public async Task SendConfirmationDoneEmail(ItemRequestModel request, int Quantity, long IdReq)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (request.Users == null) throw new ArgumentNullException(nameof(request.Users));

        var adProperties = _adUserService.GetAllAdProperties(request.Users.UserName);

        // Ambil email manager jika ada
        string? managerEmail = null;
        if (adProperties.ContainsKey("managerEmail"))
        {
            var email = adProperties["managerEmail"] as string;
            if (!string.IsNullOrEmpty(email) && !email.StartsWith("(gagal") && email != "(tidak ada email)")
            {
                managerEmail = email;
            }
        }
        // ========================
        // Ambil data dari helper
        // ========================
        List<string> fabTeamEmails = await _emailContextHelper.GetFabTeamEmailsAsync();
        UserModel? fabEngineer = await _emailContextHelper.GetEngineerAsync();
        MachineCategoriesModel? mcCategory = await _emailContextHelper.GetMachineCategoriesAsync(request.MachineCategoryId);


        string fabCategory = mcCategory?.CategoryName ?? "Unknown";

        string body = $@"
            <h3>Dear {request.Users.Name},</h3>
            <p>
                Your request for <b>{fabCategory}</b> has been completed,
                please review the items at the Inhouse Fabrication Room.
            </p>

            <table border='1' cellpadding='5' cellspacing='0'
                   style='border-collapse: collapse; width:70%; table-layout: fixed;'>
                <colgroup>
                    <col style='width: 20%;'>
                    <col style='width: 50%;'>
                </colgroup>

                <tr style='background-color:#f2f2f2;'>
                    <td><b>ID Request</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'>
                        {IdReq}
                    </td>
                </tr>

                <tr>
                    <td><b>Request Name</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'>
                        {request.PartName}
                    </td>
                </tr>

                <tr style='background-color:#f2f2f2;'>
                    <td><b>Quantity</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'>
                        {Quantity} {request.Unit}
                    </td>
                </tr>

                <tr >
                    <td><b>Status</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'>
                        <b>Finish</b>
                    </td>
                </tr>
            </table>

            <br />

             Best Regards,<br>
            {_emailSettings.SenderName}  <br><br>
            {EmailConstants.AutomatedEmailNotice} ";

        var userCC = fabTeamEmails.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        //Tambahkan email manager ke userCC jika ada dan belum ada di list
        //if (!string.IsNullOrEmpty(managerEmail) &&
        //    !userCC.Any(e => string.Equals(e, managerEmail, StringComparison.OrdinalIgnoreCase)))
        //{
        //    userCC.Add(managerEmail);
        //}

        //tambahkan engineer fabrikasi
        if (!string.IsNullOrEmpty(fabEngineer?.Email) &&
            !userCC.Any(e => string.Equals(e, fabEngineer?.Email, StringComparison.OrdinalIgnoreCase)))
        {
            userCC.Add(fabEngineer.Email);
        }



        await SendEmailAsync(
            request.Users.Email,
            $"Inhouse Fabrication Request Completed",
            body,
            userCC
        );
    }
    // ----------------------------
    // Method untuk Konfirmasi Reject
    // ----------------------------
    public async Task SendConfirmationRejectEmail(ItemRequestModel request, string Description, long IdReq)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (request.Users == null) throw new ArgumentNullException(nameof(request.Users));

        var adProperties = _adUserService.GetAllAdProperties(request.Users.UserName);

        // Ambil email manager jika ada
        string? managerEmail = null;
        if (adProperties.ContainsKey("managerEmail"))
        {
            var email = adProperties["managerEmail"] as string;
            if (!string.IsNullOrEmpty(email) && !email.StartsWith("(gagal") && email != "(tidak ada email)")
            {
                managerEmail = email;
            }
        }
        // ========================
        // Ambil data dari helper
        // ========================
        List<string> fabTeamEmails = await _emailContextHelper.GetFabTeamEmailsAsync();
        UserModel? fabEngineer = await _emailContextHelper.GetEngineerAsync();
        MachineCategoriesModel? mcCategory = await _emailContextHelper.GetMachineCategoriesAsync(request.MachineCategoryId);


        string fabCategory = mcCategory?.CategoryName ?? "Unknown";

        string body = $@"
            <h3>Dear {request.Users.Name},</h3>
            <p>
                Your request for <b>{fabCategory}</b> cannot be processed. Please review further:
            </p>

            <table border='1' cellpadding='5' cellspacing='0'
                   style='border-collapse: collapse; width:70%; table-layout: fixed;'>
                <colgroup>
                    <col style='width: 20%;'>
                    <col style='width: 50%;'>
                </colgroup>

                <tr style='background-color:#f2f2f2;'>
                    <td><b>ID Request</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'>
                        {IdReq}
                    </td>
                </tr>

                <tr>
                    <td><b>Request Name</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'>
                        {request.PartName}
                    </td>
                </tr>

                <tr style='background-color:#f2f2f2;'>
                    <td><b>Status</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'>
                        <b>Reject</b>
                    </td>
                </tr>
                <tr>
                    <td><b>Reason</b></td>
                    <td style='word-wrap: break-word; overflow-wrap: break-word;'>{Description}</td>
                </tr>
            </table>

            <br />
            Please check detail at <a href='{EmailConstants.BaseUrl}'>Inhouse Fabrication System</a>.<br><br>

             Best Regards,<br>
            {_emailSettings.SenderName}  <br><br>
            {EmailConstants.AutomatedEmailNotice} ";

        var userCC = fabTeamEmails.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        //Tambahkan email manager ke userCC jika ada dan belum ada di list
        //if (!string.IsNullOrEmpty(managerEmail) &&
        //    !userCC.Any(e => string.Equals(e, managerEmail, StringComparison.OrdinalIgnoreCase)))
        //{
        //    userCC.Add(managerEmail);
        //}

        //tambahkan engineer fabrikasi
        if (!string.IsNullOrEmpty(fabEngineer?.Email) &&
            !userCC.Any(e => string.Equals(e, fabEngineer?.Email, StringComparison.OrdinalIgnoreCase)))
        {
            userCC.Add(fabEngineer.Email);
        }

        await SendEmailAsync(
            request.Users.Email,
            $"Inhouse Fabrication Request Rejected",
            body,
            userCC
        );
    }


}
