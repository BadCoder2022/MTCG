namespace monsterTradingCardGame.BattleFunctions
{
    /// <summary>
    /// This class is used to queue two players who want to battle.
    /// </summary>
    internal class BattleLobby
    {
        private readonly object _lock = new();
        private static bool inProgress = false;
        private readonly Queue<User> userQueue = new();

        /// <summary>
        /// Joins the BattleLobby and starts the battle when two users who want to battle are present.
        /// This is also used to split two threads up, as a battle between two users should only triggered once.
        /// </summary>
        /// <param name="user">The user object of the user who wants to battle.</param>
        /// <returns>A list of 2 Users who will battle for one of the two threads who battle and an empty list for the other.</returns>
        public List<User>? JoinLobby(User user)
        {
            lock (_lock)
            {
                foreach (User u in userQueue)           //Check if a user with this name is already in the queue, to avaid a user battling with himself
                {                                       //userQueue.Contains(user) does not work due to object references.
                    if (u is null)
                    {
                        return null;
                    }

                    if (u.name == user.name)
                    {
                        return null;
                    }

                }

                User u1 = new();
                User u2 = new();

                userQueue.Enqueue(user);
                if (userQueue.Count > 1)
                {
                    u1 = userQueue.Dequeue();
                    u2 = userQueue.Dequeue();
                    Monitor.Pulse(_lock);

                }
                else
                {
                    Monitor.Wait(_lock);
                }
                List<User> list = new();

                if (!inProgress)
                {
                    inProgress = true;
                    list.Add(u1);
                    list.Add(u2);
                }
                else
                {
                    inProgress = false;
                }

                return list;
            }
        }
    }
}
