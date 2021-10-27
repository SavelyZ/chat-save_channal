// server.cpp: определяет точку входа для консольного приложения.
//

#include"stdafx.h"
#pragma comment(lib,"ws2_32.lib")
#include<winsock2.h>
#include<iostream>
#pragma warning(disable: 4996)//игнор ошибки 4996 - не критично, но можно использовать аналогичную функцию
#include "stdafx.h"
#include <string>
#include <fstream>
#include <conio.h>

//variables
SOCKET newConnection;				//новый подключеный
int count = 0;						//колличество подключений 
int a;								//счетчик масива имен(buf_name)
int msg_size;						//размер сообщения для LinkClient-Messaging

									//arrays
SOCKET *connections;				//подключенные сокеты
char *buf_name = new char[1024];	// имена клиентов подключенных(OnLine)

									//FUNCTIONS.

int messaging(int index);			//рабочий цикл 
void DeleteUserOnline(int ind);		//удаляет пользователя из списка онлайн(заменяет его на '.')
void PrintGoodMes(char* buf);		// вывод чистого сообщения
void SendAll(char* buf);			//отправить всем
int check_mes(char* buf);			//проверка сообщения на наличие команд
char* To_privateMes(char* buf);		//имя которому идет запрос на ЛС( {VASYA} ) = VASYA
int IndexReceiver(char* name);		//находит индекс подключенного по имени в списке OnLine 
void AddName(char* buf);			//добавление имени подключенного в список Online
char* NameReceiver(int index);		//находит имя по индексу в списке онлайн

int main()
{
	std::cout << "........................................SERVER IS STARTED AND READY TO WORK............................................." << std::endl;

	WSADATA wsaData;
	WORD DLLVerison = MAKEWORD(2, 2);
	if (WSAStartup(DLLVerison, &wsaData) != 0) {
		std::cout << "ERROR" << std::endl;
		exit(1);
	}
	SOCKADDR_IN addr;

	int sizeofaddr = sizeof(addr);
	addr.sin_addr.s_addr = inet_addr("26.139.167.51");//26.135.60.185
	addr.sin_port = htons(1111);
	addr.sin_family = AF_INET;

	SOCKET sListen = socket(AF_INET, SOCK_STREAM, NULL);
	bind(sListen, (SOCKADDR*)& addr, sizeof(addr));
	listen(sListen, SOMAXCONN);

	//настройка буфера с списком онлайн
	buf_name[0] = '@';
	buf_name[1] = '1';
	a = 2;
	//
	connections = (SOCKET*)calloc(64, sizeof(SOCKET));

	char i_con[] = "CONNECTED...;1;1;";
	for (;;)
	{
		newConnection = accept(sListen, (SOCKADDR*)&addr, &sizeofaddr);
		if (newConnection == 0) {
			std::cout << "ERROR CONNECTION" << std::endl;
		}
		else {
			std::cout << "NEW CONNECTION..." << std::endl;
		}
		//send(connections[count], i_con, strlen(i_con), NULL);
		connections[count] = newConnection;
		count++;
		CreateThread(NULL, NULL, (LPTHREAD_START_ROUTINE)messaging, (LPVOID)(count - 1), NULL, NULL);

	}
	return 0;
}
int messaging(int index)
{
	char* buffer = new char[1024];		//буфер для messeging
	bool f;								//наличие команды серверу - "@"+ 1 2 3 4 5 6
	int cod_com_buf;					// код команды, информация для команды
	char* mes = "understand;1;1;";		// отклик о получении команды лично клиенту(проверка)
	char* addres_man;					//имя подключенного из сообщения("@2{VASYA}") = VASYA  
	int ind;							//номер(index) человека, с которым хочет связаться данный
	char* men_discon;					//имя отключенного 
	int ind_man_dis;					//индекс отключенного по его имени
	char* man_priv_to = new char[100];  //имя клиента. делающего запрос на ЛС 
	for (;;)
	{
		//чистка буфера 0-ми
		memset(buffer, 0, sizeof(buffer));
		// получаем всегда 1024 элемента
		int m = recv(connections[index], buffer, 1024, NULL);
		if (m != 0 && m != -1)
		{
			//выведи сообщение
			PrintGoodMes(buffer);
			std::cout << std::endl;
			//проверка на наличие команд серверу
			cod_com_buf = check_mes(buffer);
			//	std::cout << cod_com_buf << std::endl;
			if (cod_com_buf == 0)
			{
				//отправка всем
				SendAll(buffer);
			}
			else
			{
				if (cod_com_buf != -1)
				{
					// отклик о получении команды лично клиенту(проверка, отправка "understand")
				//	send(connections[index], mes, strlen(mes), NULL);
					if (cod_com_buf == 1)
					{
						AddName(buffer);
						std::cout << " COMMAND ADD_NAME( ONLINE )--> ";
						//printf(buf_name);
						PrintGoodMes(buf_name);
						std::cout << std::endl;
						SendAll(buf_name);
					}



					if (cod_com_buf == 2)
					{
						addres_man = To_privateMes(buffer);// к кому личное сообщение
						man_priv_to = NameReceiver(index);
						std::cout << " COMMAND TO_PRIVATE_MASSAGE --> ";//сообщение в консоль
						PrintGoodMes(man_priv_to);
						std::cout << " --> ";
						PrintGoodMes(addres_man);//сообщение в консоль(с кем хочет связаться данный)
						ind = IndexReceiver(addres_man);
						std::cout << std::endl;
						
						char* ls_com = new char[1024];
						ls_com[0] = '@'; ls_com[1] = '2';
						int m = 0;
						for (int k = 2; k < 100; k++) 
						{
							ls_com[k] = man_priv_to[m];
							m++;
						}
						send(connections[ind], ls_com, strlen(ls_com), NULL);
						std::cout << "......."; 
						PrintGoodMes(ls_com);
						std::cout << std::endl;

					}
					if (cod_com_buf == 3)
					{
						//disconnected --> его имя в списке Online(buf_name) = '.'
						//сообщи всем о отключении
						men_discon = To_privateMes(buffer);
						ind_man_dis = IndexReceiver(men_discon);
						DeleteUserOnline(ind_man_dis);
						std::cout << "EXIT --> ";
						PrintGoodMes(buf_name);
						std::cout << std::endl;
						SendAll(buf_name);
					}
					if (cod_com_buf == 4) 
					{
						//на одном конце провода было закрыто ЛС - закрой и на другом(  @4..у кого закрой(ник) )
						addres_man = To_privateMes(buffer);// у кого закрыть
						ind = IndexReceiver(addres_man);//его индекс в списке On-line
						char* ls_Ex = new char[1024]{ "@4;1;1;" };
						send(connections[ind], ls_Ex, strlen(ls_Ex), NULL);
						std::cout <<".......DELETE_LS" <<std::endl;

					}
					if (cod_com_buf == 5) 
					{
						// перешли зашифрованное ЛС
						addres_man = To_privateMes(buffer);// кому ЛС
						ind = IndexReceiver(addres_man);//его индекс в списке On-line
						send(connections[ind], buffer, strlen(buffer), NULL);

					}
					if (cod_com_buf == 6) 
					{
						//перешли открытый ключ 
						addres_man = To_privateMes(buffer);// кому открытый ключ
						ind = IndexReceiver(addres_man);//его индекс в списке On-line
						send(connections[ind], buffer, strlen(buffer), NULL);

					}

				}
			}

		}
		else
		{
			return 0;
		}
	}
	delete(buffer);
}
void DeleteUserOnline(int ind)
{
	int indd = 0;
	for ( int i = 2; i < 1024; i++)
	{
		if ( buf_name[i] == '/') 
		{
			indd++;
		}
		if ( ind == indd ) 
		{
			if ( indd == 0 ) 
			{
				buf_name[i] = '.';
				for (int j = i + 1; j<100; j++)
				{
					if (buf_name[j] == '/')
					{
						break;
					}
					buf_name[j] = ' ';
				}
				break;
			}
			else 
			{
				buf_name[i + 1] = '.';
				for (int j = i + 2; j<100; j++)
				{
					if (buf_name[j] == '/')
					{
						break;
					}
					buf_name[j] = ' ';
				}
				break;
			}
		}
	}
}
char* NameReceiver(int index) 
{
	int in = 0;
	int s = 0;
	char* man = new char[100];
	for (int i = 2; i < 1024; i++) 
	{
		if ( buf_name[i] == '/') 
		{
			in++;
		}
		if ( in == index ) 
		{
		//	if (in == 0) 
		//	{
		//		in = 1;
		//	}
			int v = 0;
			if (in == 0) 
			{
				v = 0;
			}
			else 
			{
				v = 1;
			}
			for (int j = i+v; j<100; j++) 
			{
				if (buf_name[j] != '/') 
				{
					man[s] = buf_name[j];
					s++;
				}
				else 
				{
					break;
				}
			}
			break;
		}
	}
	man[s] = ';';
	man[s + 1] = '1';
	man[s + 2] = ';';
	man[s + 3] = '1';
	man[s + 4] = ';';
	return man;
}
int IndexReceiver(char* name)
{
	bool f = true;
	int r = -1;
	int n = 0;
	for (int i = 2; i < a; i++)
	{
		if (buf_name[i] != '/')
		{
			if (buf_name[i] == name[n] && f)
			{
				f = true;
			}
			else
			{
				f = false;
			}
		}
		n++;

		if (buf_name[i] == '/')
		{
			if (f == true)
			{
				return (r + 1);// counter ima - igor, v spiske igo = index ne vernyi !!!!BAAG
			}
			if (f == false)
			{
				r++;
				n = 0;
				f = true;
			}
		}
	}
}
char* To_privateMes(char* buf)
{
	bool f = false;
	char* ad = new char[1024];
	int ia = 0;
	for (int i = 3; i < 1024; i++)
	{
		if (buf[i] == '}')
		{
			f = true;
			break;
		}
		ad[ia] = buf[i];
		ia++;
	}
	ad[ia] = ';';
	ad[ia + 1] = '1';
	ad[ia + 2] = ';';
	return ad;
}
int check_mes(char* buf)
{
	for (int i = 0; i < 1024; i++)
	{
		if (buf[i] == '@')
		{
			switch (buf[i + 1])
			{
			case '1':
				return 1;//добавь в список онлайн
			case '2':
				return 2;//запрос на ЛС
			case '3':
				return 3;
			case '4':
				return 4;
			case '5':
				return 5;
			case '6':
				return 6;
			default:
				std::cout << "command no correct!" << std::endl;
				return-1;
			}
		}
	}
	return 0;
}
void PrintGoodMes(char* buf)
{
	for (int i = 0; i < 1024; i++)
	{
		if ((buf[i] == ';') & (buf[i + 1] == '1') & (buf[i + 2] == ';'))
			break;
		std::cout << (buf[i]);
	}
}
void SendAll(char* buf)
{
	for (int i = 0; i <= count; i++)
	{
		//просто пересылаем сообщения / выполняем стандартные обязанности сервера
		send(connections[i], buf, strlen(buf), NULL);
	}
}
void AddName(char* buf)
{
	bool f = false, f_ch_a;
	for (int i = 0; i < 1024; i++)
	{
		if (buf[i] == '}')
		{
			f = false;
			break;
		}
		if (f)
		{
			buf_name[a] = buf[i];
			a++;
		}
		if (buf[i] == '{')
		{
			f = true;
		}
	}
	// в конце метка конца ";1;1;"
	buf_name[a] = '/';
	buf_name[a + 1] = ';';
	buf_name[a + 2] = '1';
	buf_name[a + 3] = ';';
	buf_name[a + 4] = '1';
	buf_name[a + 5] = ';';
	a++;
}


