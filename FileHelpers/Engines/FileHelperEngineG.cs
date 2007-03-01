//#undef GENERICS
#define GENERICS
#if NET_2_0


#region "   Copyright 2005-06 to Marcos Meli - http://www.marcosmeli.com.ar" 

// Errors, suggestions, contributions, send a mail to: marcosdotnet[at]yahoo.com.ar.

#endregion

using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
#if GENERICS
using System.Collections.Generic;
#endif

#if ! MINI
using System.Data;
#endif


namespace FileHelpers
{



	/// <include file='FileHelperEngine.docs.xml' path='doc/FileHelperEngine/*'/>
	/// <include file='Examples.xml' path='doc/examples/FileHelperEngine/*'/>
#if ! GENERICS
	public class FileHelperEngine : EngineBase
#else
	public class FileHelperEngine<T>: EngineBase
#endif
	{

		#region "  Constructor  "

		/// <include file='FileHelperEngine.docs.xml' path='doc/FileHelperEngineCtr/*'/>
#if ! GENERICS
		public FileHelperEngine(Type recordType)
			: this(recordType, Encoding.Default)
#else
		public FileHelperEngine() 
			: this(Encoding.Default)

#endif
		{}

		/// <include file='FileHelperEngine.docs.xml' path='doc/FileHelperEngineCtr/*'/>
		/// <param name="encoding">The Encoding used by the engine.</param>
#if ! GENERICS
		public FileHelperEngine(Type recordType, Encoding encoding)
			: base(recordType, encoding)
#else
		public FileHelperEngine(Encoding encoding) 
			: base(typeof(T), encoding)
#endif
		{
			CreateRecordOptions();
		}


		private void CreateRecordOptions()
		{
			if (mRecordInfo.IsDelimited)
				mOptions = new DelimitedRecordOptions(mRecordInfo);
			else
				mOptions = new FixedRecordOptions(mRecordInfo);
		}

		internal FileHelperEngine(RecordInfo ri)
			: base(ri)
		{
			CreateRecordOptions();
		}


		#endregion

		#region "  ReadFile  "

		/// <include file='FileHelperEngine.docs.xml' path='doc/ReadFile/*'/>
#if ! GENERICS
		public object[] ReadFile(string fileName)
#else
		public T[] ReadFile(string fileName)
#endif
		{
			return ReadFile(fileName, int.MaxValue);
		}

		/// <include file='FileHelperEngine.docs.xml' path='doc/ReadFile/*'/>
		/// <param name="maxRecords">The max number of records to read. Int32.MaxValue or -1 to read all records.</param>
#if ! GENERICS
		public object[] ReadFile(string fileName, int maxRecords)
#else
		public T[] ReadFile(string fileName, int maxRecords)
#endif
		{
			using (StreamReader fs = new StreamReader(fileName, mEncoding, true))
			{
#if ! GENERICS
				object[] tempRes;
#else
				T[] tempRes;
#endif
				tempRes = ReadStream(fs, maxRecords);
				fs.Close();

				return tempRes;
			}
		}

		#endregion


		#region "  ReadStream  "

		/// <include file='FileHelperEngine.docs.xml' path='doc/ReadStream/*'/>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
#if ! GENERICS
		public object[] ReadStream(TextReader reader)
#else
		public T[] ReadStream(TextReader reader)
#endif
		{
			return ReadStream(reader, int.MaxValue);
		}

		/// <include file='FileHelperEngine.docs.xml' path='doc/ReadStream/*'/>
		/// <param name="maxRecords">The max number of records to read. Int32.MaxValue or -1 to read all records.</param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
#if ! GENERICS
		public object[] ReadStream(TextReader reader, int maxRecords)
#else
		public T[] ReadStream(TextReader reader, int maxRecords)
#endif
		{
			if (reader == null)
				throw new ArgumentNullException("reader", "The reader of the Stream cant be null");

			ResetFields();
			mHeaderText = String.Empty;
			mFooterText = String.Empty;

			ArrayList resArray = new ArrayList();
			int currentRecord = 0;

			ForwardReader freader = new ForwardReader(reader, mRecordInfo.mIgnoreLast);
			freader.DiscardForward = true;

			string currentLine, completeLine;

			mLineNumber = 1;

			completeLine = freader.ReadNextLine();
			currentLine = completeLine;

			#if !MINI
				ProgressHelper.Notify(mNotifyHandler, mProgressMode, 0, -1);
			#endif

			if (mRecordInfo.mIgnoreFirst > 0)
			{
				for (int i = 0; i < mRecordInfo.mIgnoreFirst && currentLine != null; i++)
				{
					mHeaderText += currentLine + StringHelper.NewLine;
					currentLine = freader.ReadNextLine();
					mLineNumber++;
				
				}
			}

			bool byPass = false;

			if (maxRecords < 0)
				maxRecords = int.MaxValue;

			LineInfo line = new LineInfo(currentLine);
			line.mReader = freader;
			
			while (currentLine != null && currentRecord < maxRecords)
			{
				try
				{
					mTotalRecords++;
					currentRecord++; 
				
					line.ReLoad(currentLine);
					
					bool skip = false;
					#if !MINI
						ProgressHelper.Notify(mNotifyHandler, mProgressMode, currentRecord, -1);
						skip = OnBeforeReadRecord(currentLine);
					#endif

					if (skip == false)
					{
						object record = mRecordInfo.StringToRecord(line);

						#if !MINI
							#if ! GENERICS
								skip = OnAfterReadRecord(currentLine, record);
							#else
								skip = OnAfterReadRecord(currentLine, (T) record);
							#endif
						#endif
						
						if (skip == false && record != null)
							resArray.Add(record);

					}
				}
				catch (Exception ex)
				{
					switch (mErrorManager.ErrorMode)
					{
						case ErrorMode.ThrowException:
							byPass = true;
							throw;
						case ErrorMode.IgnoreAndContinue:
							break;
						case ErrorMode.SaveAndContinue:
							ErrorInfo err = new ErrorInfo();
							err.mLineNumber = freader.LineNumber;
							err.mExceptionInfo = ex;
//							err.mColumnNumber = mColumnNum;
							err.mRecordString = completeLine;

							mErrorManager.AddError(err);
							break;
					}
				}
				finally
				{
					if (byPass == false)
					{
						currentLine = freader.ReadNextLine();
						completeLine = currentLine;
						mLineNumber++;
					}
				}

			}

			if (mRecordInfo.mIgnoreLast > 0)
			{
				mFooterText = freader.RemainingText;
			}

#if ! GENERICS
			return (object[]) 
#else
			return (T[])
#endif
			 resArray.ToArray(RecordType);
		}

		#endregion

		
		
		#region "  ReadString  "

		/// <include file='FileHelperEngine.docs.xml' path='doc/ReadString/*'/>
#if ! GENERICS
		public object[] ReadString(string source)
#else
		public T[] ReadString(string source)
#endif
		{
			return ReadString(source, int.MaxValue);
		}

		/// <include file='FileHelperEngine.docs.xml' path='doc/ReadString/*'/>
		/// <param name="maxRecords">The max number of records to read. Int32.MaxValue or -1 to read all records.</param>
#if ! GENERICS
		public object[] ReadString(string source, int maxRecords)
#else
		public T[] ReadString(string source, int maxRecords)
#endif
		{
			if (source == null)
				source = string.Empty;

			StringReader reader = new StringReader(source);
#if ! GENERICS
			object[] res;
#else
			T[] res;
#endif
			res= ReadStream(reader, maxRecords);
			reader.Close();
			return res;
		}

		#endregion

		#region "  WriteFile  "

		/// <include file='FileHelperEngine.docs.xml' path='doc/WriteFile/*'/>
#if ! GENERICS
		public void WriteFile(string fileName, IList records)
#else
		public void WriteFile(string fileName, IList<T> records)
#endif
		{
			WriteFile(fileName, records, -1);
		}

		/// <include file='FileHelperEngine.docs.xml' path='doc/WriteFile2/*'/>
#if ! GENERICS
		public void WriteFile(string fileName, IList records, int maxRecords)
#else
		public void WriteFile(string fileName, IList<T> records, int maxRecords)
#endif
		{
			using (StreamWriter fs = new StreamWriter(fileName, false, mEncoding))
			{
				WriteStream(fs, records, maxRecords);
				fs.Close();
			}

		}

		#endregion

		#region "  WriteStream  "

		/// <include file='FileHelperEngine.docs.xml' path='doc/WriteStream/*'/>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
#if ! GENERICS
		public void WriteStream(TextWriter writer, IList records)
#else
		public void WriteStream(TextWriter writer, IList<T> records)
#endif
		{
			WriteStream(writer, records, -1);
		}

		/// <include file='FileHelperEngine.docs.xml' path='doc/WriteStream2/*'/>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
#if ! GENERICS
		public void WriteStream(TextWriter writer, IList records, int maxRecords)
#else
		public void WriteStream(TextWriter writer, IList<T> records, int maxRecords)
#endif
		{
			if (writer == null)
				throw new ArgumentNullException("writer", "The writer of the Stream can be null");

			if (records == null)
				throw new ArgumentNullException("records", "The records can be null. Try with an empty array.");

			if (records.Count > 0 && records[0] != null && mRecordInfo.mRecordType.IsInstanceOfType(records[0]) == false)
				throw new BadUsageException("This engine works with record of type " + mRecordInfo.mRecordType.Name + " and you use records of type " + records[0].GetType().Name );

			ResetFields();

			if (mHeaderText != null && mHeaderText.Length != 0)
				if (mHeaderText.EndsWith(StringHelper.NewLine))
					writer.Write(mHeaderText);
				else
					writer.WriteLine(mHeaderText);


			string currentLine = null;

			//ConstructorInfo constr = mType.GetConstructor(new Type[] {});
			int max = records.Count;

			if (maxRecords >= 0)
				max = Math.Min(records.Count, maxRecords);

			#if !MINI
				ProgressHelper.Notify(mNotifyHandler, mProgressMode, 0, max);
			#endif

			for (int i = 0; i < max; i++)
			{
				try
				{
					if (records[i] == null)
						throw new BadUsageException("The record at index " + i.ToString() + " is null.");
					
					bool skip = false;
					#if !MINI
						ProgressHelper.Notify(mNotifyHandler, mProgressMode, i+1, max);
						skip = OnBeforeWriteRecord(records[i]);
					#endif

					if (skip == false)
					{
						currentLine = mRecordInfo.RecordToString(records[i]);
						#if !MINI
						currentLine = OnAfterWriteRecord(currentLine, records[i]);
						#endif
						writer.WriteLine(currentLine);
					}
				}
				catch (Exception ex)
				{
					switch (mErrorManager.ErrorMode)
					{
						case ErrorMode.ThrowException:
							throw;
						case ErrorMode.IgnoreAndContinue:
							break;
						case ErrorMode.SaveAndContinue:
							ErrorInfo err = new ErrorInfo();
							err.mLineNumber = mLineNumber;
							err.mExceptionInfo = ex;
//							err.mColumnNumber = mColumnNum;
							err.mRecordString = currentLine;
							mErrorManager.AddError(err);
							break;
					}
				}

			}

			mTotalRecords = records.Count;

			if (mFooterText != null && mFooterText != string.Empty)
				if (mFooterText.EndsWith(StringHelper.NewLine))
					writer.Write(mFooterText);
				else
					writer.WriteLine(mFooterText);

    	}

		#endregion

		#region "  WriteString  "

		/// <include file='FileHelperEngine.docs.xml' path='doc/WriteString/*'/>
#if ! GENERICS
		public string WriteString(IList records)
#else
		public string WriteString(IList<T> records)
#endif
		{
			return WriteString(records, -1);
		}

		/// <include file='FileHelperEngine.docs.xml' path='doc/WriteString2/*'/>
#if ! GENERICS
		public string WriteString(IList records, int maxRecords)
#else
		public string WriteString(IList<T> records, int maxRecords)
#endif
		{
			StringBuilder sb = new StringBuilder();
			StringWriter writer = new StringWriter(sb);
			WriteStream(writer, records, maxRecords);
			string res = writer.ToString();
			writer.Close();
			return res;
		}

		#endregion

		#region "  AppendToFile  "

		/// <include file='FileHelperEngine.docs.xml' path='doc/AppendToFile1/*'/>
#if ! GENERICS
		public void AppendToFile(string fileName, object record)
		{
			AppendToFile(fileName, new object[] {record});
		}
#else
		public void AppendToFile(string fileName, T record)
		{
			AppendToFile(fileName, new T[] {record});
		}
#endif

		/// <include file='FileHelperEngine.docs.xml' path='doc/AppendToFile2/*'/>
#if ! GENERICS
		public void AppendToFile(string fileName, IList records)
#else
		public void AppendToFile(string fileName, IList<T> records)
#endif
		{
            
            using(TextWriter writer = StreamHelper.CreateFileAppender(fileName, mEncoding, true, false))
            {
                mHeaderText = String.Empty;
                mFooterText = String.Empty;

                WriteStream(writer, records);
                writer.Close();
            }
		}

		#endregion

		#region "  DataTable Ops  "

		#if ! MINI
	
		/// <summary>
		/// Read the records of the file and fill a DataTable with them
		/// </summary>
		/// <param name="fileName">The file name.</param>
		/// <returns>The DataTable with the read records.</returns>
		public DataTable ReadFileAsDT(string fileName)
		{
			return ReadFileAsDT(fileName, -1);
		}

		/// <summary>
		/// Read the records of the file and fill a DataTable with them
		/// </summary>
		/// <param name="fileName">The file name.</param>
		/// <param name="maxRecords">The max number of records to read. Int32.MaxValue or -1 to read all records.</param>
		/// <returns>The DataTable with the read records.</returns>
		public DataTable ReadFileAsDT(string fileName, int maxRecords)
		{
			return mRecordInfo.RecordsToDataTable(ReadFile(fileName, maxRecords));
		}

		
		
		/// <summary>
		/// Read the records of a string and fill a DataTable with them.
		/// </summary>
		/// <param name="source">The source string with the records.</param>
		/// <returns>The DataTable with the read records.</returns>
		public DataTable ReadStringAsDT(string source)
		{
			return ReadStringAsDT(source, -1);
		}

		/// <summary>
		/// Read the records of a string and fill a DataTable with them.
		/// </summary>
		/// <param name="source">The source string with the records.</param>
		/// <param name="maxRecords">The max number of records to read. Int32.MaxValue or -1 to read all records.</param>
		/// <returns>The DataTable with the read records.</returns>
		public DataTable ReadStringAsDT(string source, int maxRecords)
		{
			return mRecordInfo.RecordsToDataTable(ReadString(source, maxRecords));
		}

		/// <summary>
		/// Read the records of the stream and fill a DataTable with them
		/// </summary>
		/// <param name="reader">The stream with the source records.</param>
		/// <returns>The DataTable with the read records.</returns>
		public DataTable ReadStreamAsDT(TextReader reader)
		{
			return ReadStreamAsDT(reader, -1);
		}

		/// <summary>
		/// Read the records of the stream and fill a DataTable with them
		/// </summary>
		/// <param name="reader">The stream with the source records.</param>
		/// <param name="maxRecords">The max number of records to read. Int32.MaxValue or -1 to read all records.</param>
		/// <returns>The DataTable with the read records.</returns>
		public DataTable ReadStreamAsDT(TextReader reader, int maxRecords)
		{
			return mRecordInfo.RecordsToDataTable(ReadStream(reader, maxRecords));
		}

		#endif

		#endregion


		#region "  Event Handling  "

#if ! MINI

#if NET_1_1
	/// <summary>Called in read operations just before the record string is translated to a record.</summary>
	public event BeforeReadRecordHandler BeforeReadRecord;
	/// <summary>Called in read operations just after the record was created from a record string.</summary>
	public event AfterReadRecordHandler AfterReadRecord;
	/// <summary>Called in write operations just before the record is converted to a string to write it.</summary>
	public event BeforeWriteRecordHandler BeforeWriteRecord;
	/// <summary>Called in write operations just after the record was converted to a string.</summary>
	public event AfterWriteRecordHandler AfterWriteRecord;

			
		private bool OnBeforeReadRecord(string line)
		{

			if (BeforeReadRecord != null)
			{
				BeforeReadRecordEventArgs e = null;
				e = new BeforeReadRecordEventArgs(line, LineNumber);
				BeforeReadRecord(this, e);

				return e.SkipThisRecord;
			}

			return false;
		}

		private bool OnAfterReadRecord(string line, object record)
		{
			if (mRecordInfo.mNotifyRead)
				((INotifyRead)record).AfterRead(this, line);

		    if (AfterReadRecord != null)
			{
				AfterReadRecordEventArgs e = null;
				e = new AfterReadRecordEventArgs(line, record, LineNumber);
				AfterReadRecord(this, e);
				return e.SkipThisRecord;
			}
			
			return false;
		}

		private bool OnBeforeWriteRecord(object record)
		{
			if (mRecordInfo.mNotifyWrite)
				((INotifyWrite)record).BeforeWrite(this);

		    if (BeforeWriteRecord != null)
			{
				BeforeWriteRecordEventArgs e = null;
				e = new BeforeWriteRecordEventArgs(record, LineNumber);
				BeforeWriteRecord(this, e);

				return e.SkipThisRecord;
			}

			return false;
		}

		private string OnAfterWriteRecord(string line, object record)
		{

			if (AfterWriteRecord != null)
			{
				AfterWriteRecordEventArgs e = null;
				e = new AfterWriteRecordEventArgs(record, LineNumber, line);
				AfterWriteRecord(this, e);
				return e.RecordLine;
			}
			return line;
		}

#else

#if ! GENERICS

        /// <summary>Called in read operations just before the record string is translated to a record.</summary>
        public event BeforeReadRecordHandler<object> BeforeReadRecord;
        /// <summary>Called in read operations just after the record was created from a record string.</summary>
        public event AfterReadRecordHandler<object> AfterReadRecord;
        /// <summary>Called in write operations just before the record is converted to a string to write it.</summary>
        public event BeforeWriteRecordHandler<object> BeforeWriteRecord;
        /// <summary>Called in write operations just after the record was converted to a string.</summary>
        public event AfterWriteRecordHandler<object> AfterWriteRecord;

		private bool OnBeforeReadRecord(string line)
		{

			if (BeforeReadRecord != null)
			{
				BeforeReadRecordEventArgs<object> e = null;
				e = new BeforeReadRecordEventArgs<object>(line, LineNumber);
				BeforeReadRecord(this, e);

				return e.SkipThisRecord;
			}

			return false;
		}

		private bool OnAfterReadRecord(string line, object record)
		{
			if (mRecordInfo.mNotifyRead)
				((INotifyRead)record).AfterRead(this, line);

		    if (AfterReadRecord != null)
			{
				AfterReadRecordEventArgs<object> e = null;
                e = new AfterReadRecordEventArgs<object>(line, record, LineNumber);
				AfterReadRecord(this, e);
				return e.SkipThisRecord;
			}
			
			return false;
		}

		private bool OnBeforeWriteRecord(object record)
		{
			if (mRecordInfo.mNotifyWrite)
				((INotifyWrite)record).BeforeWrite(this);

		    if (BeforeWriteRecord != null)
			{
                BeforeWriteRecordEventArgs<object> e = null;
                e = new BeforeWriteRecordEventArgs<object>(record, LineNumber);
				BeforeWriteRecord(this, e);

				return e.SkipThisRecord;
			}

			return false;
		}

		private string OnAfterWriteRecord(string line, object record)
		{

			if (AfterWriteRecord != null)
			{
                AfterWriteRecordEventArgs<object> e = null;
                e = new AfterWriteRecordEventArgs<object>(record, LineNumber, line);
				AfterWriteRecord(this, e);
				return e.RecordLine;
			}
			return line;
		}


#else
        /// <summary>Called in read operations just before the record string is translated to a record.</summary>
        public event BeforeReadRecordHandler<T> BeforeReadRecord;
        /// <summary>Called in read operations just after the record was created from a record string.</summary>
        public event AfterReadRecordHandler<T> AfterReadRecord;
        /// <summary>Called in write operations just before the record is converted to a string to write it.</summary>
        public event BeforeWriteRecordHandler<T> BeforeWriteRecord;
        /// <summary>Called in write operations just after the record was converted to a string.</summary>
        public event AfterWriteRecordHandler<T> AfterWriteRecord;

		private bool OnBeforeReadRecord(string line)
		{

			if (BeforeReadRecord != null)
			{
				BeforeReadRecordEventArgs<T> e = null;
				e = new BeforeReadRecordEventArgs<T>(line, LineNumber);
				BeforeReadRecord(this, e);

				return e.SkipThisRecord;
			}

			return false;
		}

		private bool OnAfterReadRecord(string line, T record)
		{
			if (mRecordInfo.mNotifyRead)
				((INotifyRead)record).AfterRead(this, line);

		    if (AfterReadRecord != null)
			{
				AfterReadRecordEventArgs<T> e = null;
				e = new AfterReadRecordEventArgs<T>(line, record, LineNumber);
				AfterReadRecord(this, e);
				return e.SkipThisRecord;
			}
			
			return false;
		}

		private bool OnBeforeWriteRecord(T record)
		{
			if (mRecordInfo.mNotifyWrite)
				((INotifyWrite)record).BeforeWrite(this);

		    if (BeforeWriteRecord != null)
			{
				BeforeWriteRecordEventArgs<T> e = null;
                e = new BeforeWriteRecordEventArgs<T>(record, LineNumber);
				BeforeWriteRecord(this, e);

				return e.SkipThisRecord;
			}

			return false;
		}

		private string OnAfterWriteRecord(string line, T record)
		{

			if (AfterWriteRecord != null)
			{
                AfterWriteRecordEventArgs<T> e = null;
                e = new AfterWriteRecordEventArgs<T>(record, LineNumber, line);
                AfterWriteRecord(this, e);
				return e.RecordLine;
			}
			return line;
		}

#endif

#endif

#endif

		#endregion

		internal RecordOptions mOptions;

		/// <summary>
		/// Allows to change some record layout options at runtime
		/// </summary>
		public RecordOptions Options
		{
			get { return mOptions; }
			set { mOptions = value; }
		}

	}

}


#endif