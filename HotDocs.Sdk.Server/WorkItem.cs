﻿/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HotDocs.Sdk.Server
{
	/// <summary>
	/// The type of work item -- either Interview or Document.  Each interview work items represent an interview to be presented
	/// to the end user (for which InterviewWorkItem.GetInterview, and later InterviewWorkItem.FinishInterview, must be called);
	/// each document work item represents a document to be generated (via a call to WorkSession.AssembleDocuments).
	/// </summary>
	public enum WorkItemType { 
		/// <summary>
		/// Indicates the current work item represents an interview to shown to the user.
		/// </summary>
		Interview, 

		/// <summary>
		/// Indicates the current work item represents a document assembly to be processed.
		/// </summary>
		Document 
	}

	/// <summary>
	/// WorkItem is an abstract class representing either a browser interview OR an assembled document.
	/// Work Sessions (also known as assembly queues) in the Open SDK are composed of one or more WorkItems.
	/// </summary>
	public abstract class WorkItem
	{
		/// <summary>
		/// The WorkItem constructor is protected; it is only called from derived WorkItem classes.
		/// </summary>
		/// <param name="title">The title of the work item.  If we add a Title property to a template, we would not need this.</param>
		/// <param name="template">The template upon which the work item is based.</param>
		protected WorkItem(string title, Template template)
		{
			Title = title;
			Template = template;
		}

		/* properties/state */

		// TODO: Decide if we will keep the WorkItemType enumeration, or just recommend that people use the "is" operator.
		/// <summary>
		/// The type of work item.  This enumerated property is one way to find out the type of an individual work item.
		/// (The other way is using the "is" operator.)
		/// </summary>
		abstract public WorkItemType Type { get; }

		/// <summary>
		/// The title of the work item.  This is used by a host application when presenting user interface showing the
		/// completed and pending work items.  Right now each work item maintains its own title internally, but eventually
		/// the title could be derived from elsewhere (for example, the Title property of InterviewOptions, or if that's not
		/// set then the title associated with a template).
		/// </summary>
		public string Title
		{
			get
			{
				if (_title == null)
					_title = Template.GetTitle();
				return _title;
			}
			private set{}
		}

		/// <summary>
		/// The template associated with this work item.  For interview work items, this is the template that delivers the
		/// interview.  For document work items, this is the template that generates the document.  A single template can
		/// be (and often is) associated with both an interview work item and a document work item.
		/// </summary>
		public Template Template { get; private set; }

		/// <summary>
		/// A flag indicating whether this work item has been completed or not.  This property is only set by the WorkSession
		/// to which this WorkItem belongs.
		/// </summary>
		public bool IsCompleted { get; internal set; }

		/* Other properties I thought we may need, but which are not implemented currently. */

		//public string ID { get; } // Don't know if we would want or need something like this, maybe if a host app has need to refer to a specific work item in the future?
		//public WorkSession Parent { get; private set; } // If we had this, InterviewWorkItem could fetch the default InterviewOptions from the WorkSession.
			// Without it, we can't, and that's inconvenient.  But we would need to do custom serialization to have this... maybe we'll need to anyway.
		private string _title = null;
	}

	/// <summary>
	/// An InterviewWorkItem represents an interview that will be (or has been) presented to the end user in a web browser.
	/// In includes a method to retrieve the interview HTML fragment (when delivering the interview to a browser),
	/// and another to "finish" the interview once answers have been posted back from the browser.
	/// </summary>
	[Serializable]
	public class InterviewWorkItem : WorkItem
	{
		/// <summary>
		/// The constructor is internal; it is only called from the WorkSession class.  The WorkSession
		/// is in charge of adding work items to itself.
		/// </summary>
		/// <param name="title">The title of the work item.  If we add a Title property to a template, we would not need this.</param>
		/// <param name="template">The template upon which the work item is based.</param>
		internal InterviewWorkItem(string title, Template template)
			: base(title, template)
		{
		}

		/* properties/state */

		/// <summary>
		/// Returns the work item type (document or interview) of the current object.
		/// </summary>
		override public WorkItemType Type { get { return WorkItemType.Interview; } }

		/* methods */

		/// <summary>
		/// Called by the host appliction when it needs to deliver an interview down to the end user in a web page.
		/// Wraps the underlying IServices.GetInterview call (for convenience).
		/// </summary>
		/// <param name="options">The InterviewOptions to use for this interview.  This parameter makes the WorkSession's
		/// DefaultInterviewOptions redundant/useless. TODO: We need to decide whether to use a parameter, or only the defaults.</param>
		/// <returns></returns>
		public InterviewResult GetInterview(HotDocs.Sdk.Server.Contracts.InterviewOptions options)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Called by the host application when answers have been posted back from a browser interview.
		/// </summary>
		/// <param name="interviewAnswers"></param>
		public void FinishInterview(string interviewAnswers)
		{
			// pseudocode:
			// overlay interviewAnswers over the session answer set,
			// if the current template is an interview template
			//     "assemble" it
			//     add pending assemblies to the queue as necessary
			// mark this interview workitem as complete.  (This will cause the WorkSession to advance to the next workItem.)

			throw new NotImplementedException();
		}
	}

	/// <summary>
	/// A DocumentWorkItem represents a document that needs to be (or has been) generated as part of a WorkSession.
	/// It keeps track of the template used to generate the document, and for documents that have already been generated,
	/// it also tracks the unanswered variables encountered during assembly.
	/// </summary>
	[Serializable]
	public class DocumentWorkItem : WorkItem // internally the DocumentWorkItem keeps track of where, in temporary storage, its assembled document is stored
	{
		/// <summary>
		/// The constructor is internal; it is only called from the WorkSession class (and maybe the InterviewWorkItem class).
		/// </summary>
		/// <param name="title">The title of the work item.  If we add a Title property to a template, we would not need this.</param>
		/// <param name="template">The template upon which the work item is based.</param>
		internal DocumentWorkItem(string title, Template template) : this(title, template, new string[0]) { }
		internal DocumentWorkItem(string title, Template template, string[] unansweredVariables) : base(title, template)
		{
			UnansweredVariables = unansweredVariables;
		}
		// properties/state

		/// <summary>
		/// Returns the type work item type of the current object, which is always Document in this instance.
		/// </summary>
		override public WorkItemType Type { get { return WorkItemType.Document; } }

		/// <summary>
		/// A list of variable names for which no answer was present during assembly of the document.
		/// Before an assembly has occurred, this property will be null.  After an assembly is complete,
		/// this property will be an array of strings. Each string is the name of a variable for which
		/// an answer was called for during assembly (either to merge into the document, or in the evaluation
		/// of some other expression or rule encountered during assembly), but for which no answer was
		/// present in the AnswerCollection.
		/// </summary>
		/// <remarks>
		/// Necessary because a host application may want or need this information as part of the state associated with
		/// a WorkSession.
		/// </remarks>
		public IEnumerable<string> UnansweredVariables { get; internal set; }
	}

}
