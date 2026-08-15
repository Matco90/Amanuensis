using Amanuensis.Common;
using Amanuensis.Services;
using System.Diagnostics;

string keyPressed;

DataServices dataServices = new DataServices();

Console.WriteLine(Constants.logo);

Console.WriteLine();

//string text = await dataServices.ConvertSpeechToText($@"C:\Users\matteotrevisan\Downloads\AudioTest_1.mp3");
//string text = await dataServices.ConvertSpeechToText($@"C:\Users\matteotrevisan\Downloads\VideoTest_1.mp4");

string text = $@"Non allora, I miei amici dobbiamo andare a fare una cena assieme il 19 dicembre che la facciamo ogni anno e hanno detto quest'anno dobbiamo andare tutti con I baffi.
Sì allora cosa faccio? Cosa dici? Intanto 2º te ce l'ho troppo lunga la barba?
Me la me la posso fare crescere perché l'alternativa è mi faccio crescere la barba e un'ora 1º di andare me la taglio e mi tengo I baffi per quella sera là, dopo il giorno dopo mi taglio tutto.
E va bene. Ma vabbè, ma la nonna mi deve dire cosa ne pensa. Ma sì. Una moglie che vada a vedere una festa, Quindi dici aspetta ancora un po'. Sì, sì. Sì. Provo, vediamo.
Dopo ti mando una foto che che la prossima volta mi fai un sacchettino di biscotti anche per il mio amico Stefano che gli piacciono tanto? Si può?
Dai che Va bene, gli dico che gliene fai uno, che è l'ultimo dell'anno sono a casa sua e gli porto I biscotti. Va bene? Ok.";

Console.WriteLine(text);

Console.WriteLine();

Console.WriteLine("Vuoi migliorare l'audio trascritto? (Y/N)");
keyPressed = Console.ReadLine();

Console.WriteLine();

if (keyPressed.ToLower() == "y") text = await dataServices.OptimizeAudioTranscription(text);
Console.WriteLine(text);

Console.WriteLine();

Console.WriteLine("Vuoi rissumere il contenuto del testo? (Y/N)");
keyPressed = Console.ReadLine();

Console.WriteLine();

if (keyPressed.ToLower() == "y") text = await dataServices.SummarizeText(text);
Console.WriteLine(text);

Console.WriteLine();

Console.WriteLine("Vuoi trasforamre il testo in email? (Y/N)");
keyPressed = Console.ReadLine();

Console.WriteLine();

if (keyPressed.ToLower() == "y") text = await dataServices.ConvertIntoEmail(text);
Console.WriteLine(text);

