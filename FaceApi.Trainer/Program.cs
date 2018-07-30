using Microsoft.ProjectOxford.Face;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FaceApi.Trainer
{
    class Program
    {
        private static FaceServiceClient _faceServiceClient = new FaceServiceClient("<YOUR_FACE_API_KEY>", "<YOUR_FACE_API_ENDPOINT>");

        static void Main(string[] args)
        {
            ShowInstructions();
            while (true)
            {
                string userInput = Console.ReadLine();
                Console.WriteLine();
                switch (userInput)
                {
                    case "1":
                        TrainNew();
                        break;
                    case "2":
                        TrainUpdate();
                        break;
                    case "3":
                        CreatePersonGroup();
                        break;
                    case "4":
                        ViewPersonGroup();
                        break;
                    case "5":
                        ViewPersons();
                        break;
                    case "6":
                        TestFaces();
                        break;
                    case "7":
                        DeletePersonGroups();
                        break;
                    case "8":
                        DeletePersons();
                        break;
                    case "9":
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine("Please choose between the available options only");
                        break;
                }
            }
        }

        private static void ShowInstructions()
        {
            Console.WriteLine("What do you want to do?");
            Console.WriteLine("1. Train [New]");
            Console.WriteLine("2. Train [Update]");
            Console.WriteLine("3. Create Person Group");
            Console.WriteLine("4. View All Person Group");
            Console.WriteLine("5. View All Persons");
            Console.WriteLine("6. Test Faces");
            Console.WriteLine("7. Delete Person Group");
            Console.WriteLine("8. Delete All Persons");
            Console.WriteLine("9. Exit");
        }

        private async static void DeletePersonGroups()
        {
            Console.WriteLine("Enter the Person Group Id");
            string personGroupId = Console.ReadLine();
            await _faceServiceClient.DeletePersonGroupAsync(personGroupId);
            Console.WriteLine($"Finished Deleting the person group: {personGroupId}");
        }

        private async static void DeletePersons()
        {
            Console.WriteLine("Enter the Person Group Id");
            string personGroupId = Console.ReadLine();
            var persons = await _faceServiceClient.GetPersonsAsync(personGroupId);

            if (!persons.Any())
            {
                Console.WriteLine("No Persons Registered");
                return;
            }

            Console.WriteLine($"Deleting {persons.Count()} person(s)");
            Console.WriteLine();
            foreach (var person in persons)
            {
                Console.WriteLine($"Deleting {person.Name} with PersonId: {person.PersonId}...");
                Console.WriteLine("===================================");
                await _faceServiceClient.DeletePersonAsync(personGroupId, person.PersonId);
            }
            Console.WriteLine();
            Console.WriteLine("Finished Deleting all persons");
        }

        private async static void ViewPersons()
        {
            Console.WriteLine("Enter the Person Group Id");
            string personGroupId = Console.ReadLine();
            var persons = await _faceServiceClient.GetPersonsAsync(personGroupId);

            if (!persons.Any())
            {
                Console.WriteLine("No Persons Registered");
                return;
            }

            foreach (var person in persons)
            {
                Console.WriteLine($"Person Id: {person.PersonId}");
                Console.WriteLine($"Name: {person.Name}");
                Console.WriteLine($"Persisted Face Ids: {string.Join(", ", person.PersistedFaceIds)}");
                Console.WriteLine("===================================");
            }
        }

        private async static void ViewPersonGroup()
        {
            var personGroups = await _faceServiceClient.ListPersonGroupsAsync();

            if (!personGroups.Any())
            {
                Console.WriteLine("No existing Person Groups available yet");
                return;
            }

            foreach (var personGroup in personGroups)
            {
                Console.WriteLine($"PersonGroupId: {personGroup.PersonGroupId}");
                Console.WriteLine($"Name: {personGroup.Name}");
                Console.WriteLine("===================================");
            }
        }

        private async static void CreatePersonGroup()
        {
            Console.WriteLine("Enter Person Group Id");
            string personGroupId = Console.ReadLine();

            Console.WriteLine("Enter Person Group Display Name");
            string personGroupName = Console.ReadLine();

            await _faceServiceClient.CreatePersonGroupAsync(personGroupId, personGroupName);
            Console.WriteLine($"Person Group is successfully created");
        }

        private async static void TrainNew()
        {
            Console.WriteLine("Enter the Person Group Id:");
            string personGroupId = Console.ReadLine();

            string path = @"..\..\..\Images\Train";
            foreach (var folderPath in Directory.GetDirectories(Path.GetFullPath(path)))
            {
                string folderName = folderPath.Remove(0, folderPath.LastIndexOf('\\') + 1);
                var tempArray = folderName.Split(',');
                folderName = $"{EveryFirstCharToUpper(tempArray[1].ToLower())} {EveryFirstCharToUpper(tempArray[0].ToLower())}";

                Console.WriteLine($"Creating new Person for {folderName}");

                var personResult = await _faceServiceClient.CreatePersonAsync(personGroupId, folderName);
                if (personResult != null)
                {
                    foreach (var filePath in Directory.EnumerateFiles(folderPath, "*.*"))
                    {
                        try
                        {
                            Console.WriteLine(filePath);
                            using (var fileStream = File.OpenRead(filePath))
                            {
                                var personFaceResult = await _faceServiceClient.AddPersonFaceAsync(personGroupId, personResult.PersonId, fileStream);
                                Console.WriteLine($"\t Persisted Face Id for {folderName}: {personFaceResult.PersistedFaceId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Exception thrown at image {filePath} - {ex.Message}");
                        }
                    }
                }

                if (Directory.EnumerateFiles(folderPath, "*.*").Any())
                {
                    Console.WriteLine("===================================");
                    Console.WriteLine($"Finished Adding Faces for {folderName}");
                    Console.WriteLine("===================================");
                }
                else
                    Console.WriteLine($"No available images for {folderName}");
            }

            await TrainFaces(personGroupId);
        }

        private async static void TrainUpdate()
        {
            Console.WriteLine("Enter the Person Group Id:");
            string personGroupId = Console.ReadLine();

            var persons = await _faceServiceClient.GetPersonsAsync(personGroupId);

            foreach (var person in persons)
            {
                string path = @"..\..\Images\Train\" + person.Name;
                Console.WriteLine($"Updating Person - {person.Name}");
                foreach (var filePath in Directory.EnumerateFiles(path, "*.*"))
                {
                    using (var fileStream = File.OpenRead(filePath))
                    {
                        var personFaceResult = await _faceServiceClient.AddPersonFaceAsync(personGroupId, person.PersonId, fileStream);
                        Console.WriteLine($"\t Persisted Face Id for {person.Name}: {personFaceResult.PersistedFaceId}");
                    }
                }

                if (Directory.EnumerateFiles(path, "*.*").Any())
                {
                    Console.WriteLine();
                    Console.WriteLine("===================================");
                    Console.WriteLine($"Finished Adding Faces for {person.Name}");
                    Console.WriteLine("===================================");
                    Console.WriteLine();
                }
                else
                    Console.WriteLine($"No available images for {person.Name}");
            }

            await TrainFaces(personGroupId);
        }

        private async static Task TrainFaces(string personGroupId)
        {
            await _faceServiceClient.TrainPersonGroupAsync(personGroupId);
            Console.WriteLine("Training the faces...");
            while (true)
            {
                var trainingStatus = await _faceServiceClient.GetPersonGroupTrainingStatusAsync(personGroupId);
                if (trainingStatus.Status == Microsoft.ProjectOxford.Face.Contract.Status.Succeeded)
                {
                    Console.WriteLine("Training Finished!");
                    break;
                }
                else
                    Console.WriteLine($"Training Status: {trainingStatus.Status}");
            }
        }

        private async static void TestFaces()
        {
            Console.WriteLine("Enter the Person Group Id:");
            string personGroupId = Console.ReadLine();

            string path = @"..\..\Images\Test";
            foreach (var folderPath in Directory.GetDirectories(Path.GetFullPath(path)))
            {
                string folderName = folderPath.Remove(0, folderPath.LastIndexOf('\\') + 1);

                Console.WriteLine($"Testing Face Identification for {folderName}");

                foreach (var filePath in Directory.EnumerateFiles(folderPath, "*.*"))
                {
                    using (var fileStream = File.OpenRead(filePath))
                    {
                        // always validate the first face only. Data should already be cleaned beforehand
                        var face = await _faceServiceClient.DetectAsync(fileStream);
                        var firstFace = face.FirstOrDefault();

                        if (firstFace != null)
                        {
                            try
                            {
                                var person = await _faceServiceClient.IdentifyAsync(personGroupId, new Guid[] { firstFace.FaceId });
                                var firstPerson = person.FirstOrDefault();
                                if (firstPerson != null)
                                {
                                    var personId = firstPerson.Candidates.FirstOrDefault().PersonId;
                                    var personDetail = await _faceServiceClient.GetPersonAsync(personGroupId, personId);
                                    if (personDetail != null)
                                    {
                                        Console.WriteLine($"\t Identified {personDetail.Name} with Confidence level of: {firstPerson.Candidates.FirstOrDefault().Confidence}");
                                    }
                                    else
                                        Console.WriteLine($"Person is Unknown on Image: {filePath}");
                                }
                                else
                                    Console.WriteLine($"No Person is Identified on Image: {filePath}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Exception: {ex.Message}");
                            }
                        }
                        else
                            Console.WriteLine($"No Face Detected on Image: {filePath}");
                    }
                }
            }
            Console.WriteLine("Finished Testing");
        }

        public static string EveryFirstCharToUpper(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentException("string cannot be empty!");
            }

            return Regex.Replace(input, @"(^\w)|(\s\w)", m => m.Value.ToUpper());
        }
    }
}
