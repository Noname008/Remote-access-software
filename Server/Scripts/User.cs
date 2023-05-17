namespace Server.Scripts
{
    public class User
    {
        public string Login { get; }
        public string Password { get; }
        public static Dictionary<User, List<PC>> Users { get; } = new();
        private static Dictionary<string, string> UsersDataBase { get; } = new();
        private User(string login, string password)
        {
            Login = login;
            Password = password;
            User.Users.Add(this, new List<PC>());
        }

        public static bool CreateUser(string login, string password, string ID)
        {
            User? user = null;
            try
            {
                user = GetUser(login, password);
            }
            catch (Exception)
            {
                if (CheckToDatabase(login, password))
                {
                    user = new User(login, password);
                }
            }
            finally
            {
                if(user != null)
                    _ = new PC(ID, user);
            }
            return user != null;
        }

        public static bool CreateUserToDatabase(string login, string password)
        {
            return UsersDataBase.TryAdd(login, password);
        }

        public static bool CheckToDatabase(string login, string password)
        {
            if (UsersDataBase.TryGetValue(login,out string pass) && pass == password)
                return true;
            return false;
        }

        public bool Check(string login, string password)
        {
            if(login == Login && password == Password)
                return true;
            return false;
        }

        public static User GetUser(string login, string password)
        {
            foreach(var user in Users.Keys)
            {
                if(user.Check(login,password))
                    return user;
            }
            throw new NullReferenceException();
        }
    }

    public class PC
    {
        public string ID { get; }
        private readonly User user;
        private string info = "";
        public static Dictionary<string, PC> PCs { get; } = new();

        public PC(string ID, User user)
        {
            this.ID = ID;
            this.user = user;
            User.Users[user].Add(this);
            PC.PCs.Add(ID, this);
        }

        public void SetInfo(string info)
        {
            this.info = info;
        }

        public string GetInfo() => info;

        public User GetUser() => user;

        public static void Dispose(string id)
        {
            User.Users[PCs[id].user].Remove(PCs[id]);
            if(User.Users[PCs[id].user].Count == 0)
                User.Users.Remove(PCs[id].user);
            PCs.Remove(id);
        }
    }
}
