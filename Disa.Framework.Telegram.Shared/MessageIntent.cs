using System;
using Disa.Framework;

namespace Disa.Framework.Telegram
{
	public partial class Telegram //: IMessageIntent This class should implement IMessage intent. its disabled for now, but after contact caching is done locally, this wil be updated
	{
		public string PhoneNumberToServiceAddress(string number)
		{
			var users = TelegramUtils.RunSynchronously(FetchContacts());
			var formattedNumber = FormatNumber(number);
			if (formattedNumber == null)
			{
				return null;
			}
			foreach (var user in users)
			{
				if (user.Phone == formattedNumber)
				{
					return user.Id.ToString();
				}
			}
			return null;
		}

		private string FormatNumber(string number)
		{
			var countryLocale = PhoneBook.GetCountryLocaleFromInternationalNumber(number);
			var formattedNumber = PhoneBook.FormatPhoneNumber(number, countryLocale);
			if (formattedNumber != null)
			{
				var nationalNumber = formattedNumber.Item2;
				if (formattedNumber.Item1 == "52")
				{
					if (nationalNumber.Length == 10)
					{
						nationalNumber = "1" + nationalNumber;
					}
				}
				else if (formattedNumber.Item1 == "54")
				{
					if (nationalNumber.Length == 10)
					{
						nationalNumber = "9" + nationalNumber;
					}
				}
				return "+" + formattedNumber.Item1 + nationalNumber;
			}
			else
			{
				var areaCode = PhoneBook.GetAreaCodeFromFromInternationalNumber(number, countryLocale);
				if (areaCode != null)
				{
					DebugPrint("Failed to format: " + number + ". Attempting to slam area code  " + areaCode + " in front of it...");
					var areaCodeSlammedPhoneNumber = areaCode + number;
					var formattedNumber2 = PhoneBook.FormatPhoneNumber(areaCodeSlammedPhoneNumber, countryLocale);
					if (formattedNumber2 != null)
					{
						var nationalNumber = formattedNumber2.Item2;
						if (formattedNumber2.Item1 == "52")
						{
							if (nationalNumber.Length == 10)
							{
								nationalNumber = "1" + nationalNumber;
							}
						}
						else if (formattedNumber2.Item1 == "54")
						{
							if (nationalNumber.Length == 10)
							{
								nationalNumber = "9" + nationalNumber;
							}
						}
						return "+" + formattedNumber2.Item1 + nationalNumber;
					}
					else
					{
						DebugPrint("Failed to format phone number with slammed area code. Giving up...");
						return null;
					}
				}
				else
				{
					DebugPrint("Could not resolve the area code number on the number " + number + " using locale " + countryLocale);
					return null;
				}
			}
		}
	}
}
