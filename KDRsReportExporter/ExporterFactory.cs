﻿using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Windows.Forms;

namespace KDRsReportExporter
{
    internal class ExporterFactory
    {
        private string dataSource, catalog, sp_name, SMTP, eUser, ePwd, Subject, filePath;
        private int SMTPport;
        private List<string> emailList = new List<string>();

        private Boolean isEmail;
        public SqlConnection conn;

        public ExporterFactory(Boolean isrunning)
        {
            ReadDBSettings();
            ConnectToDB(isrunning);
        }

        public string fileName { get; set; }

        public void ReadDBSettings()
        {
            try
            {
                string appPath = Application.StartupPath;
                string[] lines = System.IO.File.ReadAllLines(appPath + "\\KDRsConfig.txt");
                char demiliter = '=';
                dataSource = lines[0].Split(demiliter)[1];
                catalog = lines[1].Split(demiliter)[1];
                sp_name = lines[2].Split(demiliter)[1];
                filePath = lines[3].Split(demiliter)[1];
                fileName = lines[4].Split(demiliter)[1];
                SMTP = lines[7].Split(demiliter)[1];
                eUser = lines[8].Split(demiliter)[1];
                ePwd = lines[9].Split(demiliter)[1];
                SMTPport = int.Parse(lines[10].Split(demiliter)[1]);
                Subject = lines[11].Split(demiliter)[1];

                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }

                foreach (string line in lines)
                {
                    if (isEmail)
                    {
                        emailList.Add(line);
                    }
                    else
                    //Read the line and do the task
                    if (line.ToLower().Equals("[email]"))
                    {
                        isEmail = true;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Error" + e);
            }
        }

        public void ConnectToDB(Boolean isRunning = false)
        {
            conn = new SqlConnection();

            if (isRunning)
            {
                conn.ConnectionString =
           "Data Source=" + dataSource + ";" +

           //"Initial Catalog=" + catalog + ";" +
           "Integrated Security=True;";
            }
            else
            {
                conn.ConnectionString =
            "Data Source=" + dataSource + ";" +
            "Initial Catalog=" + catalog + ";" +
            "Integrated Security=True;";
            }

            try
            {
                conn.Open();
            }
            catch (Exception)
            {
                MessageBox.Show("DataBaseSource i KDRsConfig er feil");
            }
        }

        private DateTime startDate, endDate;

        public void setDate(DateTime _startDate, DateTime _endDate)
        {
            startDate = _startDate;

            endDate = _endDate;
        }

        public DataTable GetData()
        {
            //MessageBox.Show("We are getting data");
            DataTable dt = new DataTable();

            SqlCommand cmd = new SqlCommand(sp_name, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@startdate", startDate));
            cmd.Parameters.Add(new SqlParameter("@enddate", endDate));
            SqlDataAdapter sda = new SqlDataAdapter(cmd);
            sda.Fill(dt);

            return dt;
        }

        public void SendEmail(String attachmentFile)
        {
            //Sending the email.
            //Now we must create a new Smtp client to send our email.
            //MessageBox.Show("We are sending mail");
            SmtpClient client = new SmtpClient(SMTP, SMTPport);   //smtp.gmail.com // For Gmail
                                                                  //smtp.live.com // Windows live / Hotmail
                                                                  //smtp.mail.yahoo.com // Yahoo
                                                                  //smtp.aim.com // AIM
                                                                  //my.inbox.com // Inbox

            //Authentication.
            //This is where the valid email account comes into play. You must have a valid email account(with password) to give our program a place to send the mail from.

            NetworkCredential cred = new NetworkCredential(eUser, ePwd);

            //To send an email we must first create a new mailMessage(an email) to send.
            MailMessage Msg = new MailMessage();

            // Sender e-mail address.
            Msg.From = new MailAddress(eUser);//Nothing But Above Credentials or your credentials (*******@gmail.com)

            // Recipient e-mail address.
            foreach (String recipient in emailList)
            {
                // Msg.To.Add(recipient);
                Msg.Bcc.Add(recipient); // code for adding each email to Blindcopy copu so they do not see eachother.
            }

            // Assign the subject of our message.
            Msg.Subject = Subject;

            // Create the content(body) of our message.
            Msg.Body = "";

            Attachment attach = new Attachment(attachmentFile);

            Msg.Attachments.Add(attach);
            // Send our account login details to the client.
            client.Credentials = cred;

            //Enabling SSL(Secure Sockets Layer, encyription) is reqiured by most email providers to send mail
            client.EnableSsl = true;

            // Send our email.
            client.Send(Msg);
        }

        public string ExportToEXCEL(DataTable dt)
        {
            string fileLocation = filePath + fileName + "_" + DateTime.Today.ToShortDateString() + ".xlsx";

            PdfPCell cellH;
            iTextSharp.text.Font ColFont3 = FontFactory.GetFont(FontFactory.HELVETICA, 10, iTextSharp.text.Font.NORMAL);
            String ReportTime;
            String PrintDate = ("Utskiftdato: " + DateTime.Today.ToShortDateString());
            if (!startDate.Date.ToShortDateString().Equals("01.01.0001"))
            {
                ReportTime = ("Periode Fra : " + (startDate.ToShortDateString()) + "     Til : " + (endDate.ToShortDateString()));
            }
            else
            {
                ReportTime = ("No Date Is Set ");
            }

            XLWorkbook wb = new XLWorkbook();

            var ws = wb.Worksheets.Add(dt, "Ark1");

            ws.Row(1).InsertRowsAbove(5);
            var LstColumnUsed = ws.LastColumnUsed();

            var rngHeader = ws.Range(1, 1, 5, LstColumnUsed.ColumnNumber());
            rngHeader.Style.Fill.BackgroundColor = XLColor.LightCyan;
            rngHeader.Style.Font.Bold = true;
            ws.Cell(3, 3).Value = ReportTime;
            ws.Cell(5, LstColumnUsed.ColumnNumber()).Value = PrintDate;
            wb.SaveAs(fileLocation);
            return fileLocation;
        }

        public string ExportToPDF(DataTable dt)
        {
            //Create a dummy GridView
            //MessageBox.Show("We are exporting PDF");
            Document document = new Document();
            document.SetPageSize(iTextSharp.text.PageSize.A4.Rotate());
            string fileLocation = filePath + fileName + "_" + DateTime.Today.ToShortDateString() + ".pdf";
            PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(fileLocation, FileMode.Create));

            document.Open();
            iTextSharp.text.Font font5 = iTextSharp.text.FontFactory.GetFont(FontFactory.HELVETICA, 10);

            PdfPTable table = new PdfPTable(dt.Columns.Count);
            PdfPRow row = null;
            //float[] widths = new float[] { 4f, 4f, 4f, 4f, 4f };

            //table.SetWidths(widths);
            PdfPCell tCell;
            iTextSharp.text.Font ColFont = FontFactory.GetFont(FontFactory.HELVETICA, 15, iTextSharp.text.Font.BOLD);
            Chunk chunkCols = new Chunk(fileName, ColFont);
            tCell = new PdfPCell(new Paragraph(chunkCols));
            tCell.HorizontalAlignment = Element.ALIGN_CENTER;
            tCell.Colspan = dt.Columns.Count;
            tCell.Border = 0;
            //cell.PaddingLeft = 10;
            tCell.Padding = 5;
            tCell.PaddingTop = 0;
            table.AddCell(tCell);

            PdfPCell cellH;
            iTextSharp.text.Font ColFont3 = FontFactory.GetFont(FontFactory.HELVETICA, 10, iTextSharp.text.Font.NORMAL);
            Chunk chunkCols1;
            if (!startDate.Date.ToShortDateString().Equals("01.01.0001"))
            {
                chunkCols1 = new Chunk("Periode Fra : " + (startDate.ToShortDateString()) + "     Til : " + (endDate.ToShortDateString()), ColFont3);
            }
            else
            {
                chunkCols1 = new Chunk("No Date Is Set ");
            }

            cellH = new PdfPCell(new Paragraph(chunkCols1))
            {
                Colspan = dt.Columns.Count,
                Border = 0,
                HorizontalAlignment = Element.ALIGN_CENTER,
                PaddingTop = 10,
                PaddingBottom = 5
            };
            table.AddCell(cellH);

            //Utskrift
            chunkCols1 = new Chunk(("Utskiftdato: " + DateTime.Today.ToShortDateString()), ColFont3);
            cellH = new PdfPCell(new Paragraph(chunkCols1));
            cellH.Colspan = dt.Columns.Count;
            cellH.Border = 0;
            cellH.HorizontalAlignment = Element.ALIGN_RIGHT;
            cellH.PaddingTop = 0;
            cellH.PaddingBottom = 20;
            table.AddCell(cellH);

            table.WidthPercentage = 100;
            int iCol = 0;
            string colname = "";
            PdfPCell cell = new PdfPCell(new Phrase("Products"));
            cell.Colspan = dt.Columns.Count;

            foreach (DataColumn c in dt.Columns)
            {
                PdfPCell hcell = new PdfPCell(new Phrase(c.ColumnName, font5));
                hcell.BackgroundColor = new BaseColor(173, 216, 230);
                table.AddCell(hcell);
            }
            int row_cnt = 0;
            foreach (DataRow r in dt.Rows)
            {
                if (dt.Rows.Count > 0)
                {
                    foreach (DataColumn c in dt.Columns)
                    {
                        PdfPCell dcell = new PdfPCell(new Phrase(r[c].ToString(), font5));
                        if (row_cnt % 2 == 1)
                        {
                            dcell.BackgroundColor = new BaseColor(220, 220, 220);
                        }
                        //pcell.HorizontalAlignment = Element.ALIGN_RIGHT;

                        table.AddCell(dcell);
                    }
                    //table.AddCell(new Phrase(r[0].ToString(), font5));
                    //table.AddCell(new Phrase(r[1].ToString(), font5));
                    //table.AddCell(new Phrase(r[2].ToString(), font5));
                    //table.AddCell(new Phrase(r[3].ToString(), font5));
                    //table.AddCell(new Phrase(r[4].ToString(), font5));
                }
                row_cnt += 1;
            }

            document.Add(table);
            document.Close();
            return fileLocation;
        }
    }
}