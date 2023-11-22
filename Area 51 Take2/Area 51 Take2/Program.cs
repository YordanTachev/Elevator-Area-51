using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Area_51_Take2
{
    enum FloorType
    {
        G,
        S,
        T1,
        T2
    }
    enum SecurityLevel
    {
        Confidential,
        Secret,
        TopSecret
    }
    class Elevator
    {
        private int currentFloor = 1;
        private bool[] floorButtonsEnabled;
        private bool doorOpen = false;
        private SecurityLevel currentSecurityLevel = SecurityLevel.Confidential;
        private object elevatorLock = new object();
        private bool agentInside = false;
        private Queue<(int floor, SecurityLevel securityLevel)> requestQueue = new Queue<(int floor, SecurityLevel securityLevel)>();
        private bool servingAgent = false;
        public Elevator(int numFloors)
        {
            floorButtonsEnabled = new bool[numFloors];
            for (int i = 0; i < numFloors; i++)
            {
                floorButtonsEnabled[i] = true;
            }

            Thread elevatorThread = new Thread(ServiceElevator);
            elevatorThread.Start();
        }
        public bool IsServicingAgents()
        {
            lock (elevatorLock)
            {
                return servingAgent;
            }
        }
        public void PressFloorButton(int floor, SecurityLevel requestingAgentSecurityLevel)
        {
            lock (elevatorLock)
            {
                if (doorOpen)
                {
                    Console.WriteLine("Please close the door before selecting another floor.");
                    return;
                }

                if (agentInside && requestingAgentSecurityLevel != currentSecurityLevel)
                {
                    Console.WriteLine("Another agent is inside with a different security level. Please wait.");
                    return;
                }

                requestQueue.Enqueue((floor, requestingAgentSecurityLevel));
                Console.WriteLine($"Agent with security level {requestingAgentSecurityLevel} added request for floor {floor}.");
                //Прекъсва асансьор треда , за да прегледа заявката
                Thread.CurrentThread.Interrupt();
            }
        }
        private void ServiceElevator()
        {
            try
            {
                while (true)
                {
                    lock (elevatorLock)
                    {
                        if (servingAgent || !requestQueue.Any())
                        {
                            //асансьора се използва или чака
                            Console.WriteLine("Elevator is idle, waiting for requests.");
                            Thread.Sleep(Timeout.Infinite);
                        }

                        servingAgent = true;
                        var request = requestQueue.Dequeue();
                        var floor = request.floor;
                        var requestingAgentSecurityLevel = request.securityLevel;
                        //Симулира изчакване на асансьора
                        Thread.Sleep(1000);

                        //Симълира пристигане на етаж 
                        Console.WriteLine($"Elevator reached Floor {currentFloor}.");

                        doorOpen = CheckCredentials();

                        if (doorOpen)
                        {
                            agentInside = true;
                            Console.WriteLine("Door opening...");
                            Thread.Sleep(2000); 
                            Console.WriteLine("Door opened. You can now enter.");

                            //Симулира време за решение на кой етаж ще избере
                            Thread.Sleep(3000); 

                            Console.WriteLine("Agent exited the elevator.");
                            agentInside = false;
                        }
                        else
                        {
                            Console.WriteLine("Access denied.");
                            floorButtonsEnabled[currentFloor - 1] = true;
                        }

                        servingAgent = false;
                    }

                    //Симулира изчакване на асансьора за вход
                    Thread.Sleep(500);
                }
            }
            catch (ThreadInterruptedException)
            {
                Console.WriteLine("Elevator interrupted. Servicing...");
            }
        }
        private bool CheckCredentials()
        {
            if (currentFloor == 1) // Приземн етаж
            {
                return currentSecurityLevel == SecurityLevel.Confidential;
            }
            else if (currentFloor == 2) //Секретен
            {
                return currentSecurityLevel == SecurityLevel.Confidential || currentSecurityLevel == SecurityLevel.Secret;
            }
            else if (currentFloor == 3 || currentFloor == 4) // T1 или T2 етаж
            {
                return currentSecurityLevel == SecurityLevel.Confidential ||
                       currentSecurityLevel == SecurityLevel.Secret ||
                       currentSecurityLevel == SecurityLevel.TopSecret;
            }
            else
            {
                return false; //Няма достъп
            }
        }
        public void SetSecurityLevel(SecurityLevel securityLevel)
        {
            lock (elevatorLock)
            {
                currentSecurityLevel = securityLevel;
                Console.WriteLine($"Security level set to {securityLevel}.");
            }
        }
        public void DisplayFloorButtons()
        {
            Console.WriteLine("Available floor buttons:");
            for (int i = 0; i < floorButtonsEnabled.Length; i++)
            {
                if (floorButtonsEnabled[i])
                {
                    Console.WriteLine($"Floor {i + 1}");
                }
            }
        }
    }
    class Agent
    {
        private SecurityLevel securityLevel;
        private Elevator elevator;
        private int destinationFloor;

        public Agent(SecurityLevel level, Elevator elevator, int floor)
        {
            this.securityLevel = level;
            this.elevator = elevator;
            this.destinationFloor = floor;
        }

        public void UseElevator()
        {
            Random random = new Random();

            while (true)
            {
                lock (elevator)
                {
                    //Изчаква асансьора да е свободен
                    while (elevator.IsServicingAgents())
                    {
                        Console.WriteLine($"Agent with security level {securityLevel} is waiting for the elevator.");
                        Monitor.Wait(elevator);
                    }
                    //асансьора е свободен , избери етаж
                    elevator.SetSecurityLevel(securityLevel);
                    Console.WriteLine($"Agent with security level {securityLevel} requests floor {destinationFloor}.");
                    elevator.PressFloorButton(destinationFloor, securityLevel);
                    elevator.DisplayFloorButtons();
                }
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            int numFloors = 4; 
            Elevator elevator = new Elevator(numFloors);

            Agent confidentialAgent = new Agent(SecurityLevel.Confidential, elevator, 1);
            Agent secretAgent = new Agent(SecurityLevel.Secret, elevator, 2);
            Agent topSecretAgent = new Agent(SecurityLevel.TopSecret, elevator, 3);

            Thread t1 = new Thread(confidentialAgent.UseElevator);
            Thread t2 = new Thread(secretAgent.UseElevator);
            Thread t3 = new Thread(topSecretAgent.UseElevator);

            t1.Start();
            t2.Start();
            t3.Start();

            t1.Join();
            t2.Join();
            t3.Join();
        }
    }

}
