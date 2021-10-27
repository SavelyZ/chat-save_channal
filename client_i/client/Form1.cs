using System;using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;//потоки
using System.Runtime.InteropServices; 

namespace client
{
    public partial class Form1 : Form
    {

        [DllImport(@"C:\RSADll.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CreateKeys(ref int n, ref int t, ref int e, ref int d);

        [DllImport(@"C:\RSADll.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int encrypt(int i, int e, int n);

        [DllImport(@"C:\RSADll.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int decrypt(int i, int d, int n);

        // public static extern Keys Test(long a, long b);

        static private Socket connection;
        private IPAddress ipaddress;
        private int port;
        private Thread thr, thrLS;
        private bool f = false;
        private bool[] masF = new bool[5] { false, false, false, false, false };
        private Form2 newForm1 = new Form2();
        private int n = 3, t = 3, ee = 1, d = 5, eep = -1;// d - закрытый ключ ключ собственный ее - откр ключ, eep - открытый ключ собеседника
        private bool fKey = false;
        private bool fLsMas = false;
        private int masS = 0,m1 = 0;
        private int[] ar = new int[100];
        public Form1()
        {
            InitializeComponent();
        }
        //мои функции
        void Send_message(string message)
        {
            if (connection != null)
            {
                byte[] buffer = new byte[1024]; //динамический массив byt-ов
                buffer = Encoding.UTF8.GetBytes(message);//кодируем сообщение в набор байт
                connection.Send(buffer);
            }
            else 
            {
                this.Invoke((MethodInvoker)delegate()// исключение
                {
                    MessageBox.Show("Поключение отсутствует!","Ошибка");    
                });
            }
        }
        void CheckToSendFromForm2(Form2 form)//работа с формой ЛС
        {
            for (; ; )
            {
                this.Invoke((MethodInvoker)delegate()//исключение
                {
                    if (form.f)
                    {
                        //отправить сообщение из form2.textbox + добавить идентификатор Лисчного сообщения
                        if (form.textBox4.Text != " " && form.textBox4.Text != "")
                        {

                            string sss = form.textBox4.Text;

                            char[] arrCh = sss.ToCharArray();  
                            
                            int[] array = new  int[sss.Length];
                           // int finalScore = 0;

                            EncrBuf(arrCh,ref array, sss.Length, eep, n);

                            //     for(int ii = 0; ii < array.Length; ii++)
                            //     {
                            //         finalScore += array[ii] * Convert.ToInt32(Math.Pow(10, array.Length - ii- 1));
                            //     }
                            //   sss = finalScore.ToString();

                            int leng = sss.Length;
                            Send_message("@5{"+ form.Text  +"}{" + leng.ToString() + "};1;1;"); // передаю размер массива
                            for (int hg = 0; hg < leng; hg++)
                            {
                                Send_message("@5{" + form.Text + "}" + array[hg].ToString() + ";1;1;"); //передаю элементы массива
                                Thread.Sleep(300);
                            }
                            form.listBox1.Items.Add(textBox3.Text + ": " + form.textBox4.Text);
                            form.textBox4.Clear();
                            form.f = false;
                        }                       
                        
                    }
                    if (form.fend)
                    {
                        //  break;// закрытие ЛC / завершение потока
                        Send_message("@4" + "{" + form.Text + "}" + ";1;1;");//закрой ЛС на другом конце
                        form.fend = false;
                        form.Hide();
                        fKey = false;
            //            thrLS = null;
                        return;
                    }
                }
                ); 
            }
        }
        void Getting_message()// работает в потоке
        {
            byte[] buffer = new byte[1024];//получем буфер сатичный, всегда 1024 байт, разделение о мусора - ;1;1;
            for (int i = 0; i < buffer.Length; i++)//чистка буфера
            {
                buffer[i] = 0;
            }
            for (; ; )
            {
                try
                {
                    connection.Receive(buffer); // получаем байт-код сообщения
                    
                    string message = Encoding.UTF8.GetString(buffer); //раскодируем сообщение
                    string name_ = "";//имя пользователя из онлай при обновлении списка on-line
                    string LS = "";
                    int border = message.IndexOf(";1;1;");// найдем индекс границы мусора и сообщения
                    
                    if (border == -1)
                    {
                        continue; // если некорренкное сообщение продолжи сначала
                    }
                    string good_message = ""; // обработаем сообщение(уберем мусор)
                    for (int i = 0; i < border; i++)
                    {
                        good_message += message[i];
                    }
                    for (int i = 0; i < buffer.Length; i++)//чистка буфера
                    {
                        buffer[i] = 0;
                    }

                    int com_ = -1;
                    bool com_did = false;

                    for (int ii = 0; ii < border - 1; ii++ ) 
                    {
                        if (good_message[ii] == '@') 
                        {
                            com_did = true;
                            switch (good_message[ii + 1])
                            {
                                case '1':
                                    com_ = 1;//прислали споисок online      
                                    break;
                                case '2':
                                    com_ = 2;
                                    break;
                                case '3':
                                    com_ = 3;
                                    break;
                                case '4':
                                    com_ = 4;
                                    break;
                                case '5':
                                    com_ = 5;
                                    break;
                                case '6':
                                    com_ = 6;
                                    break;
                            }

                        }
                        if (com_did) 
                        {
                           break;                                                                                                                 
                       }
                    }

                    this.Invoke((MethodInvoker)delegate()// исключение
                    {
                        if (com_did)
                        {
                            if (com_ == -1)
                            {
                                MessageBox.Show("код команды не опознан");
                            }
                            else 
                            {           //расчленяй сообщение и добавляй онлайн
                                if (com_ == 1) 
                                {
                                    listBox2.Items.Clear();
                                    name_ = "";
                                    for (int j = 2; j < border; j++)//начинаем с 2, тк в начале - код команды '@1' 
                                    {
                                        if (good_message[j] == '/')
                                        {
                                            if ( name_[0] == '.' )
                                            {
                                                name_ = "";
                                            }
                                            else
                                            {
                                                listBox2.Items.Add(name_);
                                                name_ = "";
                                            }
                                        }
                                        else 
                                        {
                                            name_ += good_message[j];
                                        }
                                    }
                                }
                                if (com_ == 2) //ЛС
                                {
                                    name_ = "";
                                    for (int j = 2; j < border; j++)
                                    {
                                        name_ += good_message[j];
                                    }

                                    // открытие окна
                                  if (thrLS != null)
                                   {
                                        thrLS.Abort();
                                        Send_message("@4" + "{" + newForm1.Text + "}" + ";1;1;");
                                        newForm1.Hide();
                                   //     thrLS = null;
                                   }

                                    newForm1.listBox1.Items.Clear();
                                    newForm1.Show();
                                    newForm1.Text = name_;//+' '+textBox3.Text;
                                    thrLS = new Thread(delegate () { CheckToSendFromForm2(newForm1); }); //запуск потока для постоянной проверки : не нажа та ли кнопка отпревить в форме 2(newForm2.f == true), если да, то отпраляем!!!
                                    thrLS.Start();//запускаем поток

                                    //теперь по идее нужно сформировать два ключа откр и закр: первый отправить собоседнику
                                    CreateKeys(ref n, ref t, ref ee, ref d); // ee - закр, оставь себе, d - открытый отправь
                                    char[] buf_key = ee.ToString().ToCharArray();                                                                               
                                    string key_buf = new string(buf_key);
                                    Send_message("@6"+"{" + newForm1.Text + "}" + key_buf + ";1;1;");
                                    fKey = true;
                                }
                                if (com_ == 3)
                                {
                                    string name_dis = "";
                                    for (int ggjh = 1; ggjh < good_message.Length-1; ggjh++)
                                    {
                                        name_dis += good_message[ggjh];
                                    }
                                    listBox1.Items.Add(name_dis+"disconnected...");
                                }
                                if (com_ == 4)
                                {
                                    if (thrLS != null)
                                    {
                                        fKey = false;
                                        thrLS.Abort();
                                        newForm1.Hide();
                                    //    thrLS = null;
                                    }
                                }
                                if (com_ == 5)
                                {
                                    //Пришло ЛС
                                    int bls = 0;
                                    for (int s = 0; s < 1024; s++ )
                                    {
                                        if (good_message[s] == '}')
                                        {
                                            bls = s;
                                            break;
                                        }
                                    }
                                    for(int ss = bls+1; ss < border; ss++)
                                    {
                                        LS += good_message[ss];
                                    }

                                    int gBorder = LS.IndexOf("{");
                                    string lengthEMS = "";
                                    int lengthEMI;

                                    if (fLsMas)
                                    {
                                        //we in process getting messege(enc)
                                        ar[m1] = System.Convert.ToInt32(LS);
                                        m1++;
                                        masS = masS - 1;
                                        if (masS == 0)
                                        {
                                            //we get all
                                            int[] arrr = new int[m1 + 1];
                                            for (int igh = 0; igh < m1 + 1; igh++)
                                            {
                                                arrr[igh] = ar[igh];
                                            }
                                            char[] archist = new char[m1 + 1];
                                            DecrBuf(arrr, ref archist, arrr.Length, d, n);
                                            string gotov = new string(archist);
                                            newForm1.listBox1.Items.Add(newForm1.Text + " : " + gotov);
                                            fLsMas = false;
                                            m1 = 0;
                                        }
                                    }

                                    if (gBorder != -1)
                                    {

                                        for (int ghh = gBorder + 1; ghh < LS.Length-1; ghh++)
                                        {
                                            lengthEMS += LS[ghh];
                                        }
                                        lengthEMI = System.Convert.ToInt32(lengthEMS);
                                    
                                        fLsMas = true;
                                        masS = lengthEMI;
                                        m1 = 0;
                                    }   
                                    //     int[] ar = new int[LS.Length];
                                    //     char[] sk = LS.ToCharArray();
                                    //     for (int iss = 0; iss < LS.Length; iss++)
                                    //     {
                                    //        ar[iss] = (int)(sk[iss] - '0');
                                    //    }

                                    //     DecrBuf(ar, ref sk, ar.Length, d, n);//возвращает дичь
                                    //     string gotov = new string(sk);

                                    //     newForm1.listBox1.Items.Add(newForm1.Text + " : " + gotov);
                                }
                                if (com_ == 6)
                                {
                                    int exN = message.IndexOf("}");// найдем индекс конца имени( @6{имя}123445)
                                    string keyfrom = "";
                                    for (int hp = exN+1; hp < border; hp++)
                                    {
                                        keyfrom += good_message[hp];
                                    }
                               //     newForm1.listBox1.Items.Add("patners open Key - "+keyfrom);
                                    eep = System.Convert.ToInt32(keyfrom); //приняли открытый ключ собеседника, теперь отправим свой!           
                                    if ( fKey == false )
                                    {
                                       // отправка своего открытого ключа
                                        CreateKeys(ref n, ref t, ref ee, ref d); // ee - закр, оставь себе, d - открытый отправь
                                        char[] buf_key1 = ee.ToString().ToCharArray();
                                        string key_buf1 = new string(buf_key1);
                                        Send_message("@6" + "{" + newForm1.Text + "}" + key_buf1 + ";1;1;");
                                        fKey = true;
                                    }
                                }

                            }
                            
                        }
                        else 
                        {
                            listBox1.Items.Add(good_message);
                        } 
                    });

                }
                catch (Exception ex)
                {
                    //обработка в случае ошибки  
                }
            }
        }

        //зашифровать 
        void EncrBuf(char[] buf, ref int[] enc, int count, int e, int n)
        {
            for (int i = 0; i < count; i++)
            {
                enc[i] = encrypt(buf[i], e, n);
            }
        }

        //расшифровать 
        void DecrBuf(int[] buf, ref char[] dec, int count, int d, int n)
        {
            for (int i = 0; i < count; i++)
            {
                dec[i] = (char)decrypt(buf[i], d, n);
            }
        }


        //не мои функции
        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.Text = "127.0.0.1";
            textBox2.Text = "1111";

   //         CreateKeys(ref n, ref t, ref ee, ref d);
   
   //         char[] buffer = new char[3] {'1', '2', '3'};
    //        int[] enc = new int[3] {1, 1, 1};
                    //что      куда     зк
    //        EncrBuf(buffer, ref enc, 3, ee, n);

     //       DecrBuf(enc, ref buffer, 3, d, n);

      //      int gh = buffer[1];
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ipaddress = IPAddress.Parse(textBox1.Text);//26.135.60.185
            port = int.Parse(textBox2.Text);
            connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            connection.Connect(ipaddress, port);
            thr = new Thread(delegate() { Getting_message(); });
            thr.Start();//запускаем поток
            listBox1.Items.Add("connected...");
            Send_message("@1" + "{"+textBox3.Text+"}" +";1;1;"); // команда серверу(личная)
            f = true;
         // Send_message(textBox3.Text + " connected..." + ";1;1;"); //сообщение всем о подключении
        }

        private void button2_Click(object sender, EventArgs e)
        {   
            if (thr != null && f)
            {
            //  Send_message("Client "+ textBox3.Text + " disconnected" + ";1;1;");
                Send_message("@3" +"{" + textBox3.Text + "}" + ";1;1;");
                thr.Abort();
                listBox1.Items.Add("disconnected...");
                connection.Close();
                f = false;
                listBox2.Items.Clear();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox4.Text != " " && textBox4.Text != "" )
            {
                Send_message( textBox3.Text + ": " + textBox4.Text + ";1;1;");
                textBox4.Clear();
            }
            else 
            {
                MessageBox.Show("Сообщение не опознано!");
            }
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (thrLS != null)
            {
                Send_message("@4" + "{" + newForm1.Text + "}" + ";1;1;");
                fKey = false;
                thrLS.Abort();

            }
            if (thr != null && f)
            {
            // Send_message("Client " + textBox3.Text + " disconnected" + ";1;1;");
               Send_message("@3" +"{"+ textBox3.Text+"}"+ ";1;1;");
               thr.Abort();
               connection.Close();
            }
            Application.Exit();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
        
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listBox2_MouseClick(object sender, MouseEventArgs e)
        {
            int index = listBox2.IndexFromPoint(e.X, e.Y);
            if(index != -1)
            {
                contextMenuStrip1.Show(Cursor.Position);
                contextMenuStrip1.Update();
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
           
        }

        private void textBox4_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) 
            {
                if (textBox4.Text != " " && textBox4.Text != "")
                {
                    Send_message(textBox3.Text + ": " + textBox4.Text + ";1;1;");
                    textBox4.Clear();
                }
                else
                {
                    MessageBox.Show("Сообщение не опознано!");
                }
            }
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            
         //   if (e.Button == MouseButtons.Right)
          //  {
               // int index = listBox1.IndexFromPoint(e.X, e.Y);
               // if (index != -1)
                //{
                   // listBox1.SetSelected(index, true);
                    //    contextMenuStrip1.Visible = true;
             //       contextMenuStrip1.Show(Cursor.Position);
                //}
            //}
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void личноеСообщегниеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //отправить сообщение(запрос) на ЛС
            string mesLS;
            if(thrLS != null)
            {
                Send_message("@4" + "{" + newForm1.Text + "}" + ";1;1;");
                thrLS.Abort();
                newForm1.Hide();
                thrLS = null;
                fKey = false;
            }
            mesLS = listBox2.SelectedItem.ToString();
            Send_message("@2" + "{" + mesLS + "}" + ";1;1;");// отправка запроса(с кем хочу связаться)
            newForm1.listBox1.Items.Clear();
            newForm1.Show();
            newForm1.Text = mesLS;// +' '+textBox3.Text;
            thrLS = new Thread(delegate () { CheckToSendFromForm2(newForm1); }); //запуск потока для постоянной проверки : не нажа та ли кнопка отпревить в форме 2(newForm2.f == true), если да, то отпраляем!!!
            thrLS.Start();//запускаем поток

        }
    }
}
 