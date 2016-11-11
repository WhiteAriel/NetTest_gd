using System;
using Microsoft.Win32;
using System.IO;
using System.Security;
using System.Text;

namespace NetTest
{

	/// <summary>
	/// Summary description for QuickRegistry.
	/// </summary>
	public class QuickRegistry
	{

		/// <summary>
		/// was there an error?
		/// </summary>
		private bool bError;

		/// <summary>
		/// The current error message
		/// </summary>
		private StringBuilder strError;

		/// <summary>
		/// The current registry key
		/// </summary>
		private RegistryKey currentKey;

		/// <summary>
		/// keep track of the previous key
		/// </summary>
		private RegistryKey previousKey;

 
		/// <summary>
		/// Check for an error
		/// </summary>
		public bool Error
		{
			get
			{
				return bError;
			}
		}

		/// <summary>
		/// get the error message
		/// </summary>
		public string ErrorMessage
		{
			get
			{
				return strError.ToString();
			}
		}


		/// <summary>
		/// get the current registry key
		/// </summary>
		public RegistryKey GetCurrentKey
		{
			get
			{
				return currentKey;
			}
		}


		/// <summary>
		/// standard constructor
		/// </summary>
		public QuickRegistry()
		{
			//
			// TODO: Add constructor logic here
			//

			currentKey = null;
			previousKey = null;
			bError = false;
			strError = new StringBuilder();
		}



		/// <summary>
		/// open the registry key in read only or write mode
		/// </summary>
		/// <param name="registryKey">Key to open as a string "HKEY_CURRENT_USER", "HKEY_LOCAL_MACHINE"
		/// "HKEY_USERS" HKEY_CLASSES_ROOT and HKEY_CURRENT_CONFIG ommitted intentionally, if
		/// you need them add them </param>
		/// <param name="key">key to open</param>
		/// <param name="writeToKey">writeToKey equals false for readonly</param>
		/// <returns>true on success</returns>
		public bool OpenKey( string registryKey, string key, bool writeToKey )
		{
			bError = false;
			strError.Remove( 0, strError.Length );
			
			try
			{
				switch( registryKey.ToString() )
				{
					case "HKEY_CURRENT_USER": currentKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey( key, writeToKey ); break;
					case "HKEY_LOCAL_MACHINE": currentKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey( key, writeToKey ); break;
					case "HKEY_USERS": currentKey = Microsoft.Win32.Registry.Users.OpenSubKey( key, writeToKey ); break;
					default: 
					{
						strError.Append( "Invalid registry key" ); 
						bError = true; 
						return false;
					}
				}
			}
			catch( ArgumentNullException argNullExp )
			{
				bError = true;
				strError.Append( "Argument null exception thrown opening the key " + registryKey + " " + key + " Message is " + argNullExp.Message );
			}
			catch( ArgumentException argExp )
			{
				bError = true;
				strError.Append( "Argument exception thrown opening the key " + registryKey + " " + key + " Message is " + argExp.Message );
			}
			catch( UnauthorizedAccessException uaExp )
			{
				bError = true;
				strError.Append( "Unauthorised Access exception thrown opening the key " + registryKey + " " + key + " Message is " + uaExp.Message );
			}
			catch( IOException ioExp )
			{
				bError = true;
				strError.Append( "IO Exception thrown opening the key " + registryKey + " " + key + " Message is " + ioExp.Message );
			}
			catch( SecurityException secExp )
			{
				bError = true;
				strError.Append( "Security exception thrown opening the key " + registryKey + " " + key + " Message is " + secExp.Message );
			}

			return bError == true? false: true;
		}

		/// <summary>
		/// Allow the opening of a key once the current key has been set
		/// </summary>
		/// <param name="key">name of the key to open</param>
		/// <param name="writeToKey">writeToKey equals false for readonly</param>
		/// <returns>true on success</returns>
		public bool OpenKeyFromCurrentKey( string key, bool writeToKey )
		{
			bError = false;
			strError.Remove( 0, strError.Length );

			if( currentKey == null )
			{
				bError = true;
				strError.Append( "The current key is invalid when calling OpenKeyFromCurrentKey" );
				return false;
			}
			else
			{
				previousKey = currentKey;
			}

			try
			{
				currentKey = currentKey.OpenSubKey( key, writeToKey );
				if( currentKey == null )
				{
					bError = true;
					strError.Append( "Unable to open the specified key " + key + " currentKey is still valid" );
					return false;
				}

			}
			catch( ArgumentNullException argNullExp )
			{
				bError = true;
				strError.Append( "Argument null exception thrown opening the key " + key + " Message is " + argNullExp.Message );
			}
			catch( ArgumentException argExp )
			{
				bError = true;
				strError.Append( "Argument exception thrown opening the key " + key + " Message is " + argExp.Message );
			}
			catch( UnauthorizedAccessException uaExp )
			{
				bError = true;
				strError.Append( "Unauthorised Access exception thrown opening the key " + key + " Message is " + uaExp.Message );
			}
			catch( IOException ioExp )
			{
				bError = true;
				strError.Append( "IO Exception thrown opening the key " + key + " Message is " + ioExp.Message );
			}
			catch( SecurityException secExp )
			{
				bError = true;
				strError.Append( "Security exception thrown opening the key " + key + " Message is " + secExp.Message );
			}

			return bError == true? false: true;

		}


		/// <summary>
		/// Set a value on the current key
		/// </summary>
		/// <param name="name">name of the value to be set</param>
		/// <param name="value">value to set</param>
		/// <returns>true on success</returns>
		public bool SetValue( string name, object value )
		{
			bError = false;
			strError.Remove( 0, strError.Length );

			if( currentKey == null )
			{
				bError = true;
				strError.Append( "The current key was invalid when calling SetValue" );
				return false;
			}

			try
			{
				currentKey.SetValue( name, value );
			}
			catch( ArgumentNullException argNullExp )
			{
				bError = true;
				strError.Append( "Argument null exception thrown setting the value " + currentKey.ToString() + " " + name + " value = " + value.ToString() + " Message is " + argNullExp.Message );
			}
			catch( ArgumentException argExp )
			{
				bError = true;
				strError.Append( "Argument exception thrown setting the value " + currentKey.ToString() + " " + name + " value = " + value.ToString() + " Message is " + argExp.Message );
			}
			catch( UnauthorizedAccessException uaExp )
			{
				bError = true;
				strError.Append( "Unauthorised Access exception thrown setting the value " + currentKey.ToString() + " " + name + " value = " + value.ToString() + " Message is " + uaExp.Message );
			}
			catch( IOException ioExp )
			{
				bError = true;
				strError.Append( "IO Exception thrown setting the value " + currentKey.ToString() + " " + name + " value = " + value.ToString() + " Message is " + ioExp.Message );
			}
			catch( SecurityException secExp )
			{
				bError = true;
				strError.Append( "Security exception thrown setting the value " + currentKey.ToString() + " " + name + " value = " + value.ToString() + " Message is " + secExp.Message );
			}


			return bError == true? false: true;

		}

		/// <summary>
		/// get a value from the registry
		/// </summary>
		/// <param name="name">name of the value to get</param>
		/// <returns>requested value</returns>
		public object GetValue( string name )
		{
			bError = false;
			strError.Remove( 0, strError.Length );

			try
			{
				return currentKey.GetValue( name );
			}
			catch( ArgumentException argExp )
			{
				bError = true;
				strError.Append( "Argument exception thrown getting the value " + currentKey.ToString() + " " + name	  + " Message is " + argExp.Message );
			}
			catch( IOException ioExp )
			{
				bError = true;
				strError.Append( "IO Exception thrown getting the value " + currentKey.ToString() + " " + name  + " Message is " + ioExp.Message );
			}
			catch( SecurityException secExp )
			{
				bError = true;
				strError.Append( "Security exception thrown getting the value " + currentKey.ToString() + " " + name  + " Message is " + secExp.Message );
			}

			return null;
		}

		/// <summary>
		/// As a previous key is being kept allow for an easy step back
		/// </summary>
		/// <returns>true on success</returns>
		public bool RevertToPrevious()
		{
			bError = false;
			strError.Remove( 0, strError.Length );

			if( previousKey != null )
			{
				currentKey = previousKey;
			}
			else
			{
				bError = true;
				strError.Append( "Unable to revert to the previous key as it equals null" );
			}

			return bError == true? false: true;
		}

		/// <summary>
		/// Delete the current key and optionally move back to the previous key 
		/// </summary>
		/// <param name="moveBack">true to revert to the previous key</param>
		/// <param name="key">Name of the subkey to delete</param>
		/// <param name="deleteTree">delete the entire subtree from the specified key</param>
		/// <returns>returns success of deletion</returns>
		public bool DeleteKey( bool moveBack, string key, bool deleteTree )
		{
			bError = false;
			strError.Remove( 0, strError.Length );

			if( moveBack == true )
			{
				if( RevertToPrevious() == false )
					return false;
			}

			try
			{
				if( deleteTree == false )
				{
					currentKey.DeleteSubKey( key, true );
				}
				else
				{
					currentKey.DeleteSubKeyTree( key );
				}
			}
			catch( InvalidOperationException ioExp )
			{
				bError = true;
				strError.Append( "Invalid Operation Exception thrown deleting the key " + key + " " + " Message is " + ioExp.Message );
			}
			catch( ArgumentNullException argNullExp )
			{
				bError = true;
				strError.Append( "Argument null exception thrown deleting the key " + key + " "  + " Message is " + argNullExp.Message );
			}
			catch( ArgumentException argExp )
			{
				bError = true;
				strError.Append( "Argument exception thrown deleting the key " + key + " "  + " Message is " + argExp.Message );
			}
			catch( SecurityException secExp )
			{
				bError = true;
				strError.Append( "Security exception thrown deleting the key " + key + " "  + " Message is " + secExp.Message );
			}

			return bError == true? false: true;
		}



		/// <summary>
		/// delete the root key from the main registry key
		/// </summary>
		/// <param name="registryKey">Key to delete from as a string "HKEY_CURRENT_USER", "HKEY_LOCAL_MACHINE"
		/// "HKEY_USERS" HKEY_CLASSES_ROOT and HKEY_CURRENT_CONFIG ommitted intentionally, if
		/// you need them add them</param>
		/// <param name="key">name of the key to delete</param>
		/// <param name="deleteTree">Delete the entire subtree</param>
		/// <returns></returns>
		public bool DeleteRootKey( string registryKey, string key, bool deleteTree )
		{
			bError = false;
			strError.Remove( 0, strError.Length );

			try
			{
				if( deleteTree == false )
				{
					switch( registryKey.ToString() )
					{
						case "HKEY_CURRENT_USER": Microsoft.Win32.Registry.CurrentUser.DeleteSubKey( key ); break;
						case "HKEY_LOCAL_MACHINE": Microsoft.Win32.Registry.LocalMachine.DeleteSubKey( key ); break;
						case "HKEY_USERS": Microsoft.Win32.Registry.Users.DeleteSubKey( key ); break;
						default: 
						{
							strError.Append( "Invalid registry key" ); 
							bError = true; 
							return false;
						}
					}
				}
				else
				{
					switch( registryKey.ToString() )
					{
						case "HKEY_CURRENT_USER": Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree( key ); break;
						case "HKEY_LOCAL_MACHINE": Microsoft.Win32.Registry.LocalMachine.DeleteSubKeyTree( key ); break;
						case "HKEY_USERS": Microsoft.Win32.Registry.Users.DeleteSubKeyTree( key ); break;
						default: 
						{
							strError.Append( "Invalid registry key" ); 
							bError = true; 
							return false;
						}
					}
				}
			}
			catch( InvalidOperationException ioExp )
			{
				bError = true;
				strError.Append( "Invalid Operation Exception thrown deleting the key " + key + " " + " Message is " + ioExp.Message );
			}
			catch( ArgumentNullException argNullExp )
			{
				bError = true;
				strError.Append( "Argument null exception thrown deleting the key " + key + " "  + " Message is " + argNullExp.Message );
			}
			catch( ArgumentException argExp )
			{
				bError = true;
				strError.Append( "Argument exception thrown deleting the key " + key + " "  + " Message is " + argExp.Message );
			}
			catch( SecurityException secExp )
			{
				bError = true;
				strError.Append( "Security exception thrown deleting the key " + key + " "  + " Message is " + secExp.Message );
			}

			return bError == true? false: true;
		}


		/// <summary>
		/// Create key version for initial start up of project
		/// Allows the creation of a key from the root registry key
		/// eg HKEY_CURRENT_USER\YOUR COMPANY NAME
		/// </summary>
		/// <param name="registryKey">Key to open as a string "HKEY_CURRENT_USER", "HKEY_LOCAL_MACHINE"
		/// "HKEY_USERS" HKEY_CLASSES_ROOT and HKEY_CURRENT_CONFIG ommitted intentionally, if
		/// you need them add them </param>
		/// <param name="key">name of the key to create</param>
		/// <returns>true on success</returns>
		public bool CreateKey( string registryKey, string key )
		{
			bError = false;
			strError.Remove( 0, strError.Length );

			try
			{
				switch( registryKey.ToString() )
				{
					case "HKEY_CURRENT_USER": currentKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey( key ); break;
					case "HKEY_LOCAL_MACHINE": currentKey = Microsoft.Win32.Registry.LocalMachine.CreateSubKey( key ); break;
					case "HKEY_USERS": currentKey = Microsoft.Win32.Registry.Users.CreateSubKey( key ); break;
					default: 
					{
						strError.Append( "Invalid registry key" ); 
						bError = true; 
						return false;
					}
				}
			}
			catch( ArgumentNullException argNullExp )
			{
				bError = true;
				strError.Append( "Argument null exception thrown creating the key " + key  + " Message is " + argNullExp.Message );
			}
			catch( ArgumentException argExp )
			{
				bError = true;
				strError.Append( "Argument exception thrown creating the key " + key + " Message is "  + argExp.Message );
			}
			catch( UnauthorizedAccessException uaExp )
			{
				bError = true;
				strError.Append( "Unauthorised Access exception thrown creating the key " + key + " Message is " + uaExp.Message );
			}
			catch( IOException ioExp )
			{
				bError = true;
				strError.Append( "IO Exception thrown creating the key " + key + " Message is " + ioExp.Message );
			}
			catch( SecurityException secExp )
			{
				bError = true;
				strError.Append( "Security exception thrown creating the key " + key + " Message is " + secExp.Message );
			}

			return bError == true? false: true;

		}


		/// <summary>
		/// create a new registry key
		/// </summary>
		/// <param name="key">name of the key to be created</param>
		/// <param name="moveToKey"/>open the key after creating it</param>
		/// <param name="writeToKey">write permission on the key after it is open</param>
		/// <returns>true on success</returns>
		public bool CreateKey( string key, bool moveToKey, bool writeToKey )
		{

			bError = false;
			strError.Remove( 0, strError.Length );
		
			if( currentKey == null )
			{
				bError = true;
				strError.Append( "Need to open a key before one can be created from it" );
				return false;
			}
			else
			{
				previousKey = currentKey;
			}

			try
			{
				currentKey.CreateSubKey( key );
			}
			catch( ArgumentNullException argNullExp )
			{
				bError = true;
				strError.Append( "Argument null exception thrown creating the key " + key  + " Message is " + argNullExp.Message );
			}
			catch( ArgumentException argExp )
			{
				bError = true;
				strError.Append( "Argument exception thrown creating the key " + key + " Message is "  + argExp.Message );
			}
			catch( UnauthorizedAccessException uaExp )
			{
				bError = true;
				strError.Append( "Unauthorised Access exception thrown creating the key " + key + " Message is " + uaExp.Message );
			}
			catch( IOException ioExp )
			{
				bError = true;
				strError.Append( "IO Exception thrown creating the key " + key + " Message is " + ioExp.Message );
			}
			catch( SecurityException secExp )
			{
				bError = true;
				strError.Append( "Security exception thrown creating the key " + key + " Message is " + secExp.Message );
			}

			if( moveToKey == true )
				return OpenKeyFromCurrentKey( key, writeToKey );

			return bError == true? false: true;

		}


		/// <summary>
		/// close the current key
		/// </summary>
		/// <param name="closePrevious">optionally close the previous key</param>
		/// <returns>true on success</returns>
		public bool Close( bool closePrevious )
		{
			bError = false;
			strError.Remove( 0, strError.Length );

			/// check previous first
			if( closePrevious == true )
			{
				if( previousKey != null )
				{
					previousKey.Close();
					previousKey = null;
				}
			}

			if( currentKey != null )
			{
				currentKey.Close();
				currentKey = null;
			}
			else
			{
				bError = true;
				strError.Append( "Can't delete a registry when none have been opened" );
			}

			return bError == true? false: true;

		}


		/// <summary>
		/// GetSubKeyNames is the only function that doesn't return a bool
		/// this is due to the fact that it would be more irritating ( ie more function calls )
		/// to get the data
		/// </summary>
		/// <returns>a string array of the subkeys off the currentkey</returns>
		public string[] GetSubKeyNames()
		{
			bError = false;
			strError.Remove( 0, strError.Length );

			string[] subKeys = null;

			try
			{
				subKeys = currentKey.GetSubKeyNames();
			}
			catch( SecurityException secExp )
			{
				bError = true;
				strError.Append( "Security exception thrown getting the sub keys names " + secExp.Message );
				return null;
			}
			catch( IOException ioExp )
			{
				bError = true;
				strError.Append( "io exception thrown getting the sub key names " + ioExp.Message );
				return null;
			}

			return subKeys;

		}
		
	}
}
