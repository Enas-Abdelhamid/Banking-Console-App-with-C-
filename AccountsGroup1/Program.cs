using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Linq;


namespace AccountsGroup1
{
    public enum ExceptionType
    {
        ACCOUNT_DOES_NOT_EXIST,
        CREDIT_LIMIT_HAS_BEEN_EXCEEDED,
        NAME_NOT_ASSOCIATED_WITH_ACCOUNT,
        NO_OVERDRAFT,
        PASSWORD_INCORRECT,
        USER_DOES_NOT_EXIST,
        USER_NOT_LOGGED_IN
    }

    public enum AccountType
    {
        Checking,
        Saving,
        Visa
    }

    //this class relies of the following types:
    //DayTime struct and AccountType enum
    public static class Utils
    {
        static DayTime _time = new DayTime(1_048_000_000);
        static Random random = new Random();
        public static DayTime Time
        {
            get => _time += random.Next(1000);
        }
        public static DayTime Now
        {
            get => _time += 0;
        }

        public readonly static Dictionary<AccountType, string> ACCOUNT_TYPE =
            new Dictionary<AccountType, string>
        {
        { AccountType.Checking , "CK" },
        { AccountType.Saving , "SV" },
        { AccountType.Visa , "VS" }
        };

    }

    public interface ITransaction
    {
        void Withdraw(double amount, Person person);
        void Deposit(double amount, Person person);
    }

    public class AccountException : Exception
    {
        public AccountException(ExceptionType reason) : base(reason.ToString())
        {
            //  base.Message = (ExceptionType)Enum.Parse(typeof(ExceptionType), base.Message);
        }

    }

    public class LoginEventArgs : EventArgs
    {
        public string PersonName { get; set; }
        public bool Success { get; set; }
        public LoginEventArgs(string name, bool success) : base()
        {
            PersonName = name;
            Success = success;
        }
    }

    public class TransactionEventArgs : LoginEventArgs
    {
        public double Amount { get; }
        public TransactionEventArgs(string name, double amount, bool success) : base(name, success)
        {
            Amount = amount;
        }
    }

    public struct Transaction
    {
        public string AccountNumber { get; }
        public double Amount { get; }
        public Person Originator { get; }
        public DayTime Time { get; }
        public Transaction(string accountNumber, double amount, Person person, DayTime time)
        {
            AccountNumber = accountNumber;
            Amount = amount;
            Originator = person;
            Time = time;
        }
        public override string ToString()
        {

            if (Amount >= 0)
            { return $"  {AccountNumber}  ${Amount}  deposited by {Originator} on  {Utils.Time} "; }
            else if (Amount < 0)
            { return $"  {AccountNumber}  ${Amount}  withdrawn by {Originator} on  {Utils.Time} "; }
            else
                return null;
        }

    }

    public struct DayTime
    {
        private long minutes;
        public DayTime(long minutes)
        {
            this.minutes = minutes;
        }
        public static DayTime operator +(DayTime lhs, int minutes)
        {
            long lhsInMinutes = lhs.minutes; // convert from long to int
            long newMinutes = Convert.ToInt64(minutes);
            long convertedMinutes = lhsInMinutes + newMinutes;
            return new DayTime(convertedMinutes);
        }

        public override string ToString()
        {
            long calculateYears = minutes / 518400;
            long calculateMonths = (minutes % 518400) / 43200;
            long calculateDays = ((minutes % 518400) % 43200) / 1440;
            long calculateHours = (((minutes % 518400) % 43200) % 1440) / 60;
            long calculateMinutes = (((minutes % 518400) % 43200) % 1440) % 60;
            return $"  {calculateYears}-{calculateMonths.ToString("D2")}-{calculateDays.ToString("D2")}  {calculateHours.ToString("D2")}:{calculateMinutes.ToString("D2")} ";
        }
    }

    public static class Logger
    {
        private static List<string> loginEvents = new List<string>();
        private static List<string> transactionEvents = new List<string>();
        public static void LoginHandler(object sender, EventArgs args)
        {
            LoginEventArgs newArgs = args as LoginEventArgs;
            string str = "Person Name is : " + newArgs.PersonName + ". Success is :  " + newArgs.Success + " . Current Time is : " + Utils.Time;
            loginEvents.Add(str);
        }
        public static void TransactionHandler(object sender, EventArgs args)
        {
            TransactionEventArgs newArgs = args as TransactionEventArgs;
            if (newArgs.Success == true)
            {
                string str = newArgs.PersonName + " deposit  " + String.Format("{0:0,0.0}", newArgs.Amount) + " Successfully On " + Utils.Time;
                transactionEvents.Add(str);
            }
            else
            {
                string str = newArgs.PersonName + " deposit  " + String.Format("{0:0,0.0}", newArgs.Amount) + " Unuccessfully On " + Utils.Time;
                transactionEvents.Add(str);
            }
        }
        public static void ShowLoginEvents()
        {
            Console.WriteLine(Utils.Now);
            for (int index = 0; index < loginEvents.Count; index++)
            {
                var item = loginEvents[index];
                var messagePrinted = string.Format("{0}. {1}", index + 1, item);
                Console.WriteLine(messagePrinted);
            }
        }
        public static void ShowTransactionEvents()
        {
            Console.WriteLine(Utils.Now);
            for (int index = 0; index < transactionEvents.Count; index++)
            {
                var item = transactionEvents[index];
                var messagePrinted = string.Format("{0}. {1}", index + 1, item);
                Console.WriteLine(messagePrinted);
            }
        }
    }

    public class Person
    {
        // fields
        private string Password;
        public event EventHandler OnLogin;
        //properties
        public string Sin { get; }
        public string Name { get; }
        public bool IsAuthenticated { get; private set; }



        // constructor
        public Person(string name, string sin)
        {
            Name = name; Sin = sin;
            Password = Sin.Substring(0, 3);
        }

               
        public void Login(string password)
        {

            if (password != Password)
            {
                IsAuthenticated = false;
                OnLogin?.Invoke(this, new LoginEventArgs(name: Name, success: false));
                throw new AccountException(ExceptionType.PASSWORD_INCORRECT);
            }
            else
            {
                IsAuthenticated = true;
                OnLogin?.Invoke(this, new LoginEventArgs(name: Name, success: true));
            }
        }
        public void Logout()
        {
            IsAuthenticated = false;
        }
        public override string ToString()
        {
            return $"{Name}";
        }
    }


    abstract class Account
    {
        // fields
        private static int LAST_NUMBER = 100_000;
        public readonly List<Person> users = new List<Person>();
        public readonly List<Transaction> transactions = new List<Transaction>();
        public virtual event EventHandler OnLogin;


        // properties
        public string Number { get; }

        public double Balance { get; protected set; }
        public double LowestBalance { get; protected set; }

        public event EventHandler OnTransaction;
        //constructor
        public Account(string type, double balance)
        {
            Number = type + LAST_NUMBER;
            LAST_NUMBER++;
            Balance = balance;
            LowestBalance = balance;
            List<Person> users = new List<Person>();
            List<Transaction> transactions = new List<Transaction>();
        }
        //methods
        public void Deposit(double amount, Person person)
        {
            Balance += amount;
            if (Balance <=  LowestBalance)
            {
                LowestBalance = Balance;
            }
            var date = DateTime.Now;
            int noOfHours = date.Hour;
            int noOfMinutes = date.Minute;
            int totalMinutes = noOfHours * 60 + noOfMinutes;
            long minutesLong = Convert.ToInt64(totalMinutes);
            DayTime currentTime = new DayTime(minutes: minutesLong);
            Transaction t1 = new Transaction(this.Number, amount, person, currentTime);
            transactions.Add(t1);
            
        }
        public void AddUser(Person person)
        {
            users.Add(person);
        }
        public bool IsUser(string name)
        {
            foreach (Person P in users)
            {
                if (P.Name == name)
                {
                    return true;
                }

            }
            return false;
        }
        //abstract method
        public abstract void PrepareMonthlyReport();

        public virtual void OnTransactionOccur(object sender, EventArgs args)
        {
            TransactionEventArgs argsTypeCasted = args as TransactionEventArgs;
            OnTransaction?.Invoke(this, argsTypeCasted);
        }

        public override string ToString()
        {
            string s = "";
            string m = "";
            foreach (Person p in users)
            {
                s += p.Name + " , ";
            }
            foreach (Transaction t in transactions)
            {
                m += System.Environment.NewLine + t.ToString();
            }
            return $"{Number} {s} ${String.Format("{0:0,0.0}", Balance)} {m}";
        }
    }
    class CheckingAccount : Account, ITransaction
    {
        // fields
        private static double COST_PER_TRANSACTION = 0.05;

        private static double INTEREST_RATE = 0.005;

        private bool hasOverdraft;
        //constructor 
        public CheckingAccount(double balance = 0, bool hasOverdraft = false)
            : base("CK-", balance)
        {
            this.hasOverdraft = hasOverdraft;
            base.Balance = balance;
        }
        //methods
        public new void Deposit(double amount, Person person)
        {
            TransactionEventArgs referencedTransArgs = new TransactionEventArgs(name: person.Name, amount: amount, success: true);
            base.Deposit(amount, person);
            base.OnTransactionOccur(person, referencedTransArgs);
        }

        public void Withdraw(double amount, Person person)
        {
            if (!IsUser(person.Name))
            {
                TransactionEventArgs referencedTransArgs = new TransactionEventArgs(name: person.Name, amount: amount, success: false);
                base.OnTransactionOccur(person, referencedTransArgs);
                throw new AccountException(ExceptionType.NAME_NOT_ASSOCIATED_WITH_ACCOUNT);
            }
            else if (!person.IsAuthenticated)
            {
                TransactionEventArgs referencedTransArgs = new TransactionEventArgs(name: person.Name, amount: amount, success: false);
                base.OnTransactionOccur(person, referencedTransArgs);
                throw new AccountException(ExceptionType.USER_NOT_LOGGED_IN);
            }
            else if (amount > Balance && !hasOverdraft)
            {
                TransactionEventArgs referencedTransArgs = new TransactionEventArgs(name: person.Name, amount: amount, success: false);
                base.OnTransactionOccur(person, referencedTransArgs);
                throw new AccountException(ExceptionType.NO_OVERDRAFT);
            }
            else
                base.OnTransactionOccur(person, new TransactionEventArgs(name: person.Name, amount: -amount, success: true));
            base.Deposit(-amount, person);
        }
        public override void PrepareMonthlyReport()
        {
            double serviceCharge = COST_PER_TRANSACTION * transactions.Count;
            double interest = (LowestBalance * INTEREST_RATE) / 12.00;
            Balance = (Balance + interest) - serviceCharge;
            transactions.Clear();
        }
    }
    class SavingAccount : Account, ITransaction
    {
        // fields
        private static double COST_PER_TRANSACTION = 0.05;

        private static double INTEREST_RATE = 0.015;

        // constructor
        public SavingAccount(double balance = 0)
            : base("SV-", balance)
        {
            base.Balance = balance;

        }
        // method
        public new void Deposit(double amount, Person person)
        {
            TransactionEventArgs referencedTransArgs = new TransactionEventArgs(name: person.Name, amount: amount, success: true);
            base.Deposit(amount, person);
            base.OnTransactionOccur(person, referencedTransArgs);
        }
        public void Withdraw(double amount, Person person)
        {
            if (!IsUser(person.Name))
            {
                TransactionEventArgs referencedTransArgs = new TransactionEventArgs(name: person.Name, amount: amount, success: false);
                base.OnTransactionOccur(person, referencedTransArgs);
                throw new AccountException(ExceptionType.NAME_NOT_ASSOCIATED_WITH_ACCOUNT);
            }
            else if (!person.IsAuthenticated)
            {
                TransactionEventArgs referencedTransArgs = new TransactionEventArgs(name: person.Name, amount: amount, success: false);
                base.OnTransactionOccur(person, referencedTransArgs);
                throw new AccountException(ExceptionType.USER_NOT_LOGGED_IN);
            }
            else if (amount > Balance)
            {
                TransactionEventArgs referencedTransArgs = new TransactionEventArgs(name: person.Name, amount: amount, success: false);
                base.OnTransactionOccur(person, referencedTransArgs);
                throw new AccountException(ExceptionType.NO_OVERDRAFT);
            }
            else
                base.OnTransactionOccur(person, new TransactionEventArgs(name: person.Name, amount: -amount, success: true));
            base.Deposit(-amount, person);
        }
        public override void PrepareMonthlyReport()
        {
            double serviceCharge = COST_PER_TRANSACTION * transactions.Count;
            double interest = (LowestBalance * INTEREST_RATE) / 12.00;
            Balance = (Balance + interest) - serviceCharge;
            transactions.Clear();
        }
    }
    class VisaAccount : Account
    {
        //fields
        private double creditLimit;
        private static double INTEREST_RATE = 0.1995;
        //constructor
        public VisaAccount(double balance = 0, double creditLimit = 1200)
            : base("VS-", balance)
        {
            base.Balance = balance;
            this.creditLimit = creditLimit;
        }
        //methods
        public void DoPayment(double amount, Person person)
        {

            TransactionEventArgs referencedTransArgs = new TransactionEventArgs(name: person.Name, amount: amount, success: true);
            base.Deposit(amount, person);
            base.OnTransactionOccur(person, referencedTransArgs);
        }
        public void DoPurchase(double amount, Person person)
        {
            if (!IsUser(person.Name))
            {
                TransactionEventArgs referencedTransArgs = new TransactionEventArgs(name: person.Name, amount: amount, success: false);
                base.OnTransactionOccur(person, referencedTransArgs);
                throw new AccountException(ExceptionType.NAME_NOT_ASSOCIATED_WITH_ACCOUNT);
            }
            else if (!person.IsAuthenticated)
            {
                TransactionEventArgs referencedTransArgs = new TransactionEventArgs(name: person.Name, amount: amount, success: false);
                base.OnTransactionOccur(person, referencedTransArgs);
                throw new AccountException(ExceptionType.USER_NOT_LOGGED_IN);
            }
            else if (amount > Balance)
            {
                TransactionEventArgs referencedTransArgs = new TransactionEventArgs(name: person.Name, amount: amount, success: false);
                base.OnTransactionOccur(person, referencedTransArgs);
                throw new AccountException(ExceptionType.NO_OVERDRAFT);
            }
            else
                base.OnTransactionOccur(person, new TransactionEventArgs(name: person.Name, amount: -amount, success: true));
            base.Deposit(-amount, person);
        }
        public override void PrepareMonthlyReport()
        {
            double interest = (LowestBalance * INTEREST_RATE) / 12.00;
            Balance = Balance - interest;
            transactions.Clear();
        }

        

        static class Bank
        {
            public static readonly Dictionary<string, Account> ACCOUNTS = new Dictionary<string, Account>();
            public static readonly Dictionary<string, Person> USERS = new Dictionary<string, Person>();
            static Bank()
            {
                //initialize the USERS collection
                AddPerson("Narendra", "1234-5678");    //0
                AddPerson("Ilia", "2345-6789");        //1
                AddPerson("Mehrdad", "3456-7890");     //2
                AddPerson("Vinay", "4567-8901");       //3
                AddPerson("Arben", "5678-9012");       //4
                AddPerson("Patrick", "6789-0123");     //5
                AddPerson("Yin", "7890-1234");         //6
                AddPerson("Hao", "8901-2345");         //7
                AddPerson("Jake", "9012-3456");        //8
                AddPerson("Mayy", "1224-5678");        //9
                AddPerson("Nicoletta", "2344-6789");   //10


                //initialize the ACCOUNTS collection
                AddAccount(new VisaAccount());              //VS-100000
                AddAccount(new VisaAccount(150, -500));     //VS-100001
                AddAccount(new SavingAccount(5000));        //SV-100002
                AddAccount(new SavingAccount());            //SV-100003
                AddAccount(new CheckingAccount(2000));      //CK-100004
                AddAccount(new CheckingAccount(1500, true));//CK-100005
                AddAccount(new VisaAccount(50, -550));      //VS-100006
                AddAccount(new SavingAccount(1000));        //SV-100007 

                //associate users with accounts
                string number = "VS-100000";
                AddUserToAccount(number, "Narendra");
                AddUserToAccount(number, "Ilia");
                AddUserToAccount(number, "Mehrdad");


                number = "VS-100001";
                AddUserToAccount(number, "Vinay");
                AddUserToAccount(number, "Arben");
                AddUserToAccount(number, "Patrick");

                number = "SV-100002";
                AddUserToAccount(number, "Yin");
                AddUserToAccount(number, "Hao");
                AddUserToAccount(number, "Jake");

                number = "SV-100003";
                AddUserToAccount(number, "Mayy");
                AddUserToAccount(number, "Nicoletta");

                number = "CK-100004";
                AddUserToAccount(number, "Mehrdad");
                AddUserToAccount(number, "Arben");
                AddUserToAccount(number, "Yin");

                number = "CK-100005";
                AddUserToAccount(number, "Jake");
                AddUserToAccount(number, "Nicoletta");

                number = "VS-100006";
                AddUserToAccount(number, "Ilia");
                AddUserToAccount(number, "Vinay");

                number = "SV-100007";
                AddUserToAccount(number, "Patrick");
                AddUserToAccount(number, "Hao");
            }
            public static void PrintAccounts()
            {

                foreach (var item in ACCOUNTS)
                {
                    Console.WriteLine(item.Value);
                }
            }


            public static void PrintPersons()
            {
                foreach (var item in USERS)
                {
                    Console.WriteLine(item);
                }
            }

            public static Person GetPerson(string name)
            {
                if (USERS.ContainsKey(name))
                {
                    Person fetchedPersonFromDic = USERS[name];
                    return fetchedPersonFromDic;
                }
                else
                    throw new AccountException(ExceptionType.USER_DOES_NOT_EXIST);

            }

            public static Account GetAccount(string number)
            {

                if (ACCOUNTS.ContainsKey(number))
                {
                    Account fetchedAccountFromDic = ACCOUNTS[number];
                    return fetchedAccountFromDic;
                }
                else
                    throw new AccountException(ExceptionType.ACCOUNT_DOES_NOT_EXIST);


            }

            public static void AddPerson(string name, string sin)
            {
                Person currentPerson = new Person(name: name, sin: sin);
                currentPerson.OnLogin += Logger.LoginHandler;
                USERS.Add(name, currentPerson);
            }

            public static void AddAccount(Account account)
            {
                account.OnTransaction += Logger.TransactionHandler;
                ACCOUNTS.Add(account.Number, account);
            }

            public static void AddUserToAccount(string number, string name)
            {
                Account foundAccountFromDic = ACCOUNTS[number];
                Person foundPersonFromDic = USERS[name];
                Boolean result = number.Contains("VS");
                if (result)
                {
                    VisaAccount obV = foundAccountFromDic as VisaAccount;
                    obV.AddUser(foundPersonFromDic);
                }
                result = number.Contains("SV");
                if (result)
                {
                    SavingAccount obS = foundAccountFromDic as SavingAccount;
                    obS.AddUser(foundPersonFromDic);
                }
                result = number.Contains("CK");
                if (result)
                {
                    CheckingAccount obC = foundAccountFromDic as CheckingAccount;
                    obC.AddUser(foundPersonFromDic);
                }





            }

            public static List<Transaction> GetAllTransactions()
            {
                List<Transaction> collectiveFetchedTransactions = new List<Transaction>();
                List<Transaction> fetchedTransactions1 = new List<Transaction>();
                List<Transaction> fetchedTransactions2 = new List<Transaction>();
                List<Transaction> fetchedTransactions3 = new List<Transaction>();

                foreach (var item in ACCOUNTS)
                {

                    Boolean result = item.Key.Contains("VS");
                    if (result)
                    {
                        VisaAccount obV = item.Value as VisaAccount;
                        fetchedTransactions1 = obV.transactions;

                    }
                    result = item.Key.Contains("SV");
                    if (result)
                    {
                        SavingAccount obS = item.Value as SavingAccount;
                        fetchedTransactions2 = obS.transactions;
                    }
                    result = item.Key.Contains("CK");
                    if (result)
                    {
                        CheckingAccount obC = item.Value as CheckingAccount;
                        fetchedTransactions3 = obC.transactions;
                    }

                    foreach (var j in fetchedTransactions1)
                    { collectiveFetchedTransactions.Add(j); }
                    foreach (var i in fetchedTransactions2)
                    { collectiveFetchedTransactions.Add(i); }
                    foreach (var k in fetchedTransactions3)
                    { collectiveFetchedTransactions.Add(k); }


                }

                return collectiveFetchedTransactions;

            }

        }
        class Program
        {
            static void Main(string[] args)
            {

                Console.WriteLine("\nAll acounts:");
                Bank.PrintAccounts();
                Console.WriteLine("\nAll Users:");
                Bank.PrintPersons();

                Person p0, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10;
                p0 = Bank.GetPerson("Narendra");
                p1 = Bank.GetPerson("Ilia");
                p2 = Bank.GetPerson("Mehrdad");
                p3 = Bank.GetPerson("Vinay");
                p4 = Bank.GetPerson("Arben");
                p5 = Bank.GetPerson("Patrick");
                p6 = Bank.GetPerson("Yin");
                p7 = Bank.GetPerson("Hao");
                p8 = Bank.GetPerson("Jake");
                p9 = Bank.GetPerson("Mayy");
                p10 = Bank.GetPerson("Nicoletta");

                p0.Login("123"); p1.Login("234");
                p2.Login("345"); p3.Login("456");
                p4.Login("567"); p5.Login("678");
                p6.Login("789"); p7.Login("890");
                p10.Login("234"); p8.Login("901");

                //a visa account
                VisaAccount a = Bank.GetAccount("VS-100000") as VisaAccount;
                a.DoPayment(1500, p0);
                a.DoPurchase(200, p1);
                a.DoPurchase(25, p2);
                a.DoPurchase(15, p0);
                a.DoPurchase(39, p1);
                a.DoPayment(400, p0);
                Console.WriteLine(a);

                a = Bank.GetAccount("VS-100001") as VisaAccount;
                a.DoPayment(500, p0);
                a.DoPurchase(25, p3);
                a.DoPurchase(20, p4);
                a.DoPurchase(15, p5);
                Console.WriteLine(a);

                //a saving account
                SavingAccount b = Bank.GetAccount("SV-100002") as SavingAccount;
                b.Withdraw(300, p6);
                b.Withdraw(32.90, p6);
                b.Withdraw(50, p7);
                b.Withdraw(111.11, p8);
                Console.WriteLine(b);

                b = Bank.GetAccount("SV-100003") as SavingAccount;
                b.Deposit(300, p3);     //ok even though p3 is not a holder
                b.Deposit(32.90, p2);
                b.Deposit(50, p5);
                b.Withdraw(111.11, p10);
                Console.WriteLine(b);

                //a checking account
                CheckingAccount c = Bank.GetAccount("CK-100004") as CheckingAccount;
                c.Deposit(33.33, p7);
                c.Deposit(40.44, p7);
                c.Withdraw(150, p2);
                c.Withdraw(200, p4);
                c.Withdraw(645, p6);
                c.Withdraw(350, p6);
                Console.WriteLine(c);

                c = Bank.GetAccount("CK-100005") as CheckingAccount;
                c.Deposit(33.33, p8);
                c.Deposit(40.44, p7);
                c.Withdraw(450, p10);
                c.Withdraw(500, p8);
                c.Withdraw(645, p10);
                c.Withdraw(850, p10);
                Console.WriteLine(c);

                a = Bank.GetAccount("VS-100006") as VisaAccount;
                a.DoPayment(700, p0);
                a.DoPurchase(20, p3);
                a.DoPurchase(10, p1);
                a.DoPurchase(15, p1);
                Console.WriteLine(a);

                b = Bank.GetAccount("SV-100007") as SavingAccount;
                b.Deposit(300, p3);     //ok even though p3 is not a holder
                b.Deposit(32.90, p2);
                b.Deposit(50, p5);
                b.Withdraw(111.11, p7);
                Console.WriteLine(b);

                Console.WriteLine("\n\nExceptions:");
                //The following will cause exception
                try
                {
                    p8.Login("911");            //incorrect password
                }
                catch (AccountException e) { Console.WriteLine(e.Message); }

                try
                {
                    p3.Logout();
                    a.DoPurchase(12.5, p3);     //exception user is not logged in
                }
                catch (AccountException e) { Console.WriteLine(e.Message); }

                try
                {
                    a.DoPurchase(12.5, p0);     //user is not associated with this account
                }
                catch (AccountException e) { Console.WriteLine(e.Message); }

                try
                {
                    a.DoPurchase(5825, p4);     //credit limit exceeded
                }
                catch (AccountException e) { Console.WriteLine(e.Message); }
                try
                {
                    c.Withdraw(1500, p6);       //no overdraft
                }
                catch (AccountException e) { Console.WriteLine(e.Message); }

                try
                {
                    Bank.GetAccount("CK-100018"); //account does not exist
                }
                catch (AccountException e) { Console.WriteLine(e.Message); }

                try
                {
                    Bank.GetPerson("Trudeau");  //user does not exist
                }
                catch (AccountException e) { Console.WriteLine(e.Message); }

                //show all transactions
                Console.WriteLine("\n\nAll transactions");
                foreach (var transaction in Bank.GetAllTransactions())
                    Console.WriteLine(transaction);
                foreach (var keyValuePair in Bank.ACCOUNTS)
                {
                    Account account = keyValuePair.Value;
                    Console.WriteLine("\nBefore PrepareMonthlyReport()");
                    Console.WriteLine(account);

                    Console.WriteLine("\nAfter PrepareMonthlyReport()");
                    account.PrepareMonthlyReport();   //all transactions are cleared, balance changes
                    Console.WriteLine(account);
                }

                Logger.ShowLoginEvents();
                Logger.ShowTransactionEvents();





            }
        }
    }
}



