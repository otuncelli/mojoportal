﻿// Author:					i7MEDIA (joe davis)
// Created:				    2014-12-22
// Last Modified:			2019-04-03
//
// You must not remove this notice, or any other, from this software.
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;
using log4net;
using mojoPortal.Business;
using mojoPortal.Business.WebHelpers;
using mojoPortal.SearchIndex;
using mojoPortal.Web;
using mojoPortal.Web.Framework;
using Newtonsoft.Json;
using SuperFlexiBusiness;

namespace SuperFlexiUI
{
    public partial class Widget : UserControl
    {
        #region Properties
        private static readonly ILog log = LogManager.GetLogger(typeof(Widget));
		private List<Field> fields = new List<Field>();
        private string moduleTitle = string.Empty;
        private string markupErrorFormat = "SuperFlexi markup definition error when rendering {0} for {1}. Error was {2}";
        StringBuilder strOutput = new StringBuilder();
        StringBuilder strAboveMarkupScripts = new StringBuilder();
        StringBuilder strBelowMarkupScripts = new StringBuilder();
        List<Item> items = new List<Item>();
        List<ItemFieldValue> fieldValues = new List<ItemFieldValue>();
		List<ModuleConfiguration> moduleConfigs = new List<ModuleConfiguration>();
        SiteSettings siteSettings;
        PageSettings pageSettings;
        Module module;
		public ModuleConfiguration Config { get; set; } = new ModuleConfiguration();
		public string SiteRoot { get; set; } = string.Empty;
		public string ImageSiteRoot { get; set; } = string.Empty;
		public bool IsEditable { get; set; } = false;
		public int ModuleId { get; set; } = -1;
		public int PageId { get; set; } = -1;

		#endregion

		protected void Page_Load(object sender, EventArgs e)
        {
            //LoadSettings();
            //SetupScripts();

            module = new Module(ModuleId);
            moduleTitle = module.ModuleTitle;

            siteSettings = CacheHelper.GetCurrentSiteSettings();
            pageSettings = new PageSettings(siteSettings.SiteId, PageId);
            if (Config.MarkupDefinition != null)
            {
                displaySettings = Config.MarkupDefinition;
            }

			if (Config.ProcessItems)
			{
				fields = Field.GetAllForDefinition(Config.FieldDefinitionGuid);
			
				if (Config.IsGlobalView)
				{
					items = Item.GetAllForDefinition(Config.FieldDefinitionGuid, siteSettings.SiteGuid, Config.DescendingSort);
					fieldValues = ItemFieldValue.GetItemValuesByDefinition(Config.FieldDefinitionGuid);
				}
				else
				{
					items = Item.GetModuleItems(ModuleId, Config.DescendingSort);
					fieldValues = ItemFieldValue.GetItemValuesByModule(module.ModuleGuid);
				}
			}

			if (SiteUtils.IsMobileDevice() && Config.MobileMarkupDefinition != null)
            {
                displaySettings = Config.MobileMarkupDefinition;
            }

            if (Config.MarkupScripts.Count > 0 || (SiteUtils.IsMobileDevice() && Config.MobileMarkupScripts.Count > 0))
            {

                if (SiteUtils.IsMobileDevice() && Config.MobileMarkupScripts.Count > 0)
                {
                    SuperFlexiHelpers.SetupScripts(Config.MobileMarkupScripts, Config, displaySettings, siteSettings.UseSslOnAllPages, IsEditable, IsPostBack, ClientID, ModuleId, PageId, Page, this);
                }
                else
                {
                    SuperFlexiHelpers.SetupScripts(Config.MarkupScripts, Config, displaySettings, siteSettings.UseSslOnAllPages, IsEditable, IsPostBack, ClientID, ModuleId, PageId, Page, this);
                }

            }

            if (Config.MarkupCSS.Count > 0)
            {
                SuperFlexiHelpers.SetupStyle(Config.MarkupCSS, Config, displaySettings, siteSettings.UseSslOnAllPages, ClientID, ModuleId, PageId, Page, this);
            }

            //if (Page.IsPostBack) { return; }

            PopulateControls();

        }

        private void PopulateControls()
        {
            string featuredImageUrl = string.Empty;
            string markupTop = string.Empty;
            string markupBottom = string.Empty;

            featuredImageUrl = String.IsNullOrWhiteSpace(Config.InstanceFeaturedImage) ? featuredImageUrl : SiteUtils.GetNavigationSiteRoot() + Config.InstanceFeaturedImage;
            markupTop = displaySettings.ModuleInstanceMarkupTop;
            markupBottom = displaySettings.ModuleInstanceMarkupBottom;

            strOutput.Append(markupTop);

            if (Config.UseHeader && Config.HeaderLocation == "InnerBodyPanel" && !String.IsNullOrWhiteSpace(Config.HeaderContent) && !String.Equals(Config.HeaderContent, "<p>&nbsp;</p>"))
            {
                try
                {

                    strOutput.Append(string.Format(displaySettings.HeaderContentFormat, Config.HeaderContent));

                }
                catch (FormatException ex)
                {
                    log.ErrorFormat(markupErrorFormat, "HeaderContentFormat", moduleTitle, ex);
                }
            }
            StringBuilder jsonString = new StringBuilder();
            StringWriter stringWriter = new StringWriter(jsonString);
            JsonTextWriter jsonWriter = new JsonTextWriter(stringWriter);

            // http://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_DateTimeZoneHandling.htm
            jsonWriter.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
            // http://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_DateFormatHandling.htm
            jsonWriter.DateFormatHandling = DateFormatHandling.IsoDateFormat;

            string jsonObjName = "sflexi" + module.ModuleId.ToString() + (Config.IsGlobalView ? "Modules" : "Items");
            if (Config.RenderJSONOfData)
            {

                jsonWriter.WriteRaw("var " + jsonObjName + " = ");
                if (Config.JsonLabelObjects || Config.IsGlobalView)
                {
                    jsonWriter.WriteStartObject();
                }
                else
                {
                    jsonWriter.WriteStartArray();
                }

            }

            List<IndexedStringBuilder> itemsMarkup = new List<IndexedStringBuilder>();
            //List<Item> categorizedItems = new List<Item>();
            bool usingGlobalViewMarkup = !String.IsNullOrWhiteSpace(displaySettings.GlobalViewMarkup);
            int currentModuleID = -1;
            foreach (Item item in items)
            {
                bool itemIsEditable = IsEditable || WebUser.IsInRoles(item.EditRoles);
				bool itemIsViewable = itemIsEditable || WebUser.IsAdminOrContentAdminOrContentPublisherOrContentAuthor || WebUser.IsInRoles(item.ViewRoles);
                if (!itemIsViewable)
                {
                    continue;
                }

                //int itemCount = 0;
                //StringBuilder content = new StringBuilder();
                IndexedStringBuilder content = new IndexedStringBuilder();

				ModuleConfiguration itemModuleConfig = Config;

				if (Config.IsGlobalView)
				{
					itemModuleConfig = new ModuleConfiguration(module);
					content.SortOrder1 = itemModuleConfig.GlobalViewSortOrder;
                    content.SortOrder2 = item.SortOrder;
				}
                else
                {
                    content.SortOrder1 = item.SortOrder;
                }

                item.ModuleFriendlyName = itemModuleConfig.ModuleFriendlyName;
                if (String.IsNullOrWhiteSpace(itemModuleConfig.ModuleFriendlyName))
                {
                    Module itemModule = new Module(item.ModuleGuid);
                    if (itemModule != null)
                    {
                        item.ModuleFriendlyName = itemModule.ModuleTitle;
                    }

                }

                //List<ItemFieldValue> fieldValues = ItemFieldValue.GetItemValues(item.ItemGuid);

                //using item.ModuleID here because if we are using a 'global view' we need to be sure the item edit link uses the correct module id.
                string itemEditUrl = SiteUtils.GetNavigationSiteRoot() + "/SuperFlexi/Edit.aspx?pageid=" + PageId + "&mid=" + item.ModuleID + "&itemid=" + item.ItemID;
                string itemEditLink = itemIsEditable ? String.Format(displaySettings.ItemEditLinkFormat, itemEditUrl) : string.Empty;

                if (Config.RenderJSONOfData)
                {
                    if (Config.IsGlobalView)
                    {
                        if (currentModuleID != item.ModuleID)
                        {
                            if (currentModuleID != -1)
                            {
                                jsonWriter.WriteEndObject();
                                jsonWriter.WriteEndObject();
                            }

                            currentModuleID = item.ModuleID;

                            //always label objects in globalview
                            jsonWriter.WritePropertyName("m" + currentModuleID.ToString());
                            jsonWriter.WriteStartObject();
                            jsonWriter.WritePropertyName("Module");
                            jsonWriter.WriteValue(item.ModuleFriendlyName);
                            jsonWriter.WritePropertyName("Items");
                            jsonWriter.WriteStartObject();
                        }


                    }
                    if (Config.JsonLabelObjects || Config.IsGlobalView) jsonWriter.WritePropertyName("i" + item.ItemID.ToString());
                    jsonWriter.WriteStartObject();
                    jsonWriter.WritePropertyName("ItemId");
                    jsonWriter.WriteValue(item.ItemID.ToString());
                    jsonWriter.WritePropertyName("SortOrder");
                    jsonWriter.WriteValue(item.SortOrder.ToString());
                    if (IsEditable)
                    {
                        jsonWriter.WritePropertyName("EditUrl");
                        jsonWriter.WriteValue(itemEditUrl);
                    }
                }
                content.Append(displaySettings.ItemMarkup);


				foreach (Field field in fields)
                {
					//if (!WebUser.IsInRoles(field.ViewRoles))
					//{
					//	continue;
					//}

					if (String.IsNullOrWhiteSpace(field.Token)) field.Token = "$_NONE_$"; //just in case someone has loaded the database with fields without using a source file. 

                    bool fieldValueFound = false;

					var itemFieldValues = fieldValues.Where(fv => fv.ItemGuid == item.ItemGuid);
					var tokens = fields.Select(x => new { x.Token, x.FieldGuid, x.PreTokenString, x.PostTokenString, x.PreTokenStringWhenFalse, x.PreTokenStringWhenTrue, x.PostTokenStringWhenFalse, x.PostTokenStringWhenTrue, x.ControlType });


					foreach (ItemFieldValue fieldValue in itemFieldValues)
                    {
                        if (field.FieldGuid == fieldValue.FieldGuid)
                        {
                            fieldValueFound = true;

                            if (String.IsNullOrWhiteSpace(fieldValue.FieldValue) ||
                                fieldValue.FieldValue.StartsWith("&deleted&") ||
                                fieldValue.FieldValue.StartsWith("&amp;deleted&amp;") ||
                                fieldValue.FieldValue.StartsWith("<p>&deleted&</p>") ||
                                fieldValue.FieldValue.StartsWith("<p>&amp;deleted&amp;</p>") ||
								(!WebUser.IsAdminOrContentAdminOrContentPublisherOrContentAuthor &&
								!WebUser.IsInRoles(field.ViewRoles)))
                            {

                                content.Replace("^" + field.Token + "^", string.Empty);
                                content.Replace("^" + field.Token, string.Empty);
                                content.Replace(field.Token + "^", string.Empty);
                                content.Replace(field.Token, string.Empty);
                            }
                            else
                            {
                                if (IsDateField(field))
                                {
                                    DateTime dateTime = new DateTime();
                                    if (DateTime.TryParse(fieldValue.FieldValue, out dateTime))
                                    {
                                        /// ^field.Token is used when we don't want the preTokenString and postTokenString to be used
                                        content.Replace("^" + field.Token + "^", dateTime.ToString(field.DateFormat));
                                        content.Replace("^" + field.Token, dateTime.ToString(field.DateFormat) + field.PostTokenString);
                                        content.Replace(field.Token + "^", field.PreTokenString + dateTime.ToString(field.DateFormat));
                                        content.Replace(field.Token, field.PreTokenString + dateTime.ToString(field.DateFormat) + field.PostTokenString);
                                    }
                                }

                                if (IsCheckBoxListField(field) || IsRadioButtonListField(field))
                                {
                                    foreach (CheckBoxListMarkup cblm in Config.CheckBoxListMarkups)
                                    {
                                        if (cblm.Field == field.Name)
                                        {
                                            StringBuilder cblmContent = new StringBuilder();

                                            List<string> values = fieldValue.FieldValue.SplitOnCharAndTrim(';');
                                            if (values.Count > 0)
                                            {
                                                foreach (string value in values)
                                                {
                                                    //why did we use _ValueItemID_ here instead of _ItemID_?
                                                    cblmContent.Append(cblm.Markup.Replace(field.Token, value).Replace("$_ValueItemID_$", item.ItemID.ToString()) + cblm.Separator);
                                                    cblm.SelectedValues.Add(new CheckBoxListMarkup.SelectedValue { Value = value, ItemID = item.ItemID });
                                                    //cblm.SelectedValues.Add(fieldValue);
                                                }
                                            }
                                            cblmContent.Length -= cblm.Separator.Length;
                                            content.Replace(cblm.Token, cblmContent.ToString());
                                        }
                                    }                                    
                                }

								if (field.ControlType == "CheckBox")
								{
									string checkBoxContent = string.Empty;

									if (fieldValue.FieldValue == field.CheckBoxReturnValueWhenTrue)
									{
										content.Replace("^" + field.Token + "^", fieldValue.FieldValue);
										content.Replace("^" + field.Token, fieldValue.FieldValue + field.PostTokenString + field.PostTokenStringWhenTrue);
										content.Replace(field.Token + "^", field.PreTokenString + field.PreTokenStringWhenTrue + fieldValue.FieldValue);
										content.Replace(field.Token, field.PreTokenString + field.PreTokenStringWhenTrue + fieldValue.FieldValue + field.PostTokenString + field.PostTokenStringWhenTrue);
									}

									else if (fieldValue.FieldValue == field.CheckBoxReturnValueWhenFalse)
									{
										content.Replace("^" + field.Token + "^", fieldValue.FieldValue);
										content.Replace("^" + field.Token, fieldValue.FieldValue + field.PostTokenString + field.PostTokenStringWhenFalse);
										content.Replace(field.Token + "^", field.PreTokenString + field.PreTokenStringWhenFalse + fieldValue.FieldValue);
										content.Replace(field.Token, field.PreTokenString + field.PreTokenStringWhenFalse + fieldValue.FieldValue + field.PostTokenString + field.PostTokenStringWhenFalse);
									}
								}

                                // ^field.Token^ is used when we don't want the preTokenString and postTokenString to be used
                                content.Replace("^" + field.Token + "^", fieldValue.FieldValue);
                                content.Replace("^" + field.Token, fieldValue.FieldValue + field.PostTokenString);
                                content.Replace(field.Token + "^", field.PreTokenString + fieldValue.FieldValue);
                                content.Replace(field.Token, field.PreTokenString + fieldValue.FieldValue + field.PostTokenString);

								//We want any tokens used in our pre or post token strings to be replaced. 
								//todo: add controlType specific logic to be sure tokens used in pre and post are replaced with proper formatting (i.e.: date field)
								List<string> prePostTokenStrings = new List<string>();

								if (!String.IsNullOrWhiteSpace(field.PreTokenString))
								{
									prePostTokenStrings.Add(field.PreTokenString);
								}

								if (!String.IsNullOrWhiteSpace(field.PostTokenString))
								{
									prePostTokenStrings.Add(field.PostTokenString);
								}

								if (!String.IsNullOrWhiteSpace(field.PreTokenStringWhenTrue))
								{
									prePostTokenStrings.Add(field.PreTokenStringWhenTrue);
								}

								if (!String.IsNullOrWhiteSpace(field.PreTokenStringWhenFalse))
								{
									prePostTokenStrings.Add(field.PreTokenStringWhenFalse);
								}

								if (!String.IsNullOrWhiteSpace(field.PostTokenStringWhenTrue))
								{
									prePostTokenStrings.Add(field.PostTokenStringWhenTrue);
								}

								if (!String.IsNullOrWhiteSpace(field.PostTokenStringWhenFalse))
								{
									prePostTokenStrings.Add(field.PostTokenStringWhenFalse);
								}

								var sharedTokens = tokens.Where(token => prePostTokenStrings.Any(tokenString => tokenString.Contains(token.Token))).ToList();

								//foreach (var token in tokens)
								//{
								//	if (prePostTokenStrings.Contains(token.Token))
								//	{
								//		sharedTokens.Add(token);
								//	}
								//}

								foreach (var token in sharedTokens)
								{
									var sharedTokenFieldValue = fieldValues.Where(x => x.FieldGuid == token.FieldGuid && x.ItemGuid == item.ItemGuid).Select(y => y.FieldValue).Single();

									//content.Replace(token.Token, );


									//if (token.ControlType == "CheckBox")
									//{
									//	string checkBoxContent = string.Empty;

									//	if (sharedTokenFieldValue == field.CheckBoxReturnValueWhenTrue)
									//	{
									//		content.Replace("^" + field.Token + "^", fieldValue.FieldValue);
									//		content.Replace("^" + field.Token, fieldValue.FieldValue + field.PostTokenString + field.PostTokenStringWhenTrue);
									//		content.Replace(field.Token + "^", field.PreTokenString + field.PreTokenStringWhenTrue + fieldValue.FieldValue);
									//		content.Replace(field.Token, field.PreTokenString + field.PreTokenStringWhenTrue + fieldValue.FieldValue + field.PostTokenString + field.PostTokenStringWhenTrue);
									//	}

									//	else if (fieldValue.FieldValue == field.CheckBoxReturnValueWhenFalse)
									//	{
									//		content.Replace("^" + field.Token + "^", fieldValue.FieldValue);
									//		content.Replace("^" + field.Token, fieldValue.FieldValue + field.PostTokenString + field.PostTokenStringWhenFalse);
									//		content.Replace(field.Token + "^", field.PreTokenString + field.PreTokenStringWhenFalse + fieldValue.FieldValue);
									//		content.Replace(field.Token, field.PreTokenString + field.PreTokenStringWhenFalse + fieldValue.FieldValue + field.PostTokenString + field.PostTokenStringWhenFalse);
									//	}
									//}
									if (!String.IsNullOrWhiteSpace(sharedTokenFieldValue))
									{
										content.Replace("^" + token.Token + "^", sharedTokenFieldValue);
										content.Replace("^" + token.Token, sharedTokenFieldValue + token.PostTokenString);
										content.Replace(token.Token + "^", token.PreTokenString + sharedTokenFieldValue);
										content.Replace(token.Token, token.PreTokenString + sharedTokenFieldValue + token.PostTokenString);
									}
								}

                            }
                            //if (!String.IsNullOrWhiteSpace(field.LinkedField))
                            //{
                            //    Field linkedField = fields.Find(delegate(Field f) { return f.Name == field.LinkedField; });
                            //    if (linkedField != null)
                            //    {
                            //        ItemFieldValue linkedValue = fieldValues.Find(delegate(ItemFieldValue fv) { return fv.FieldGuid == linkedField.FieldGuid; });
                            //        content.Replace(linkedField.Token, linkedValue.FieldValue);
                            //    }
                            //}

                            if (Config.RenderJSONOfData && 
								(WebUser.IsAdminOrContentAdminOrContentPublisherOrContentAuthor || WebUser.IsInRoles(field.ViewRoles)))
                            {
                                jsonWriter.WritePropertyName(field.Name);
                                //if (IsDateField(field))
                                //{
                                //    DateTime dateTime = new DateTime();
                                //    if (DateTime.TryParse(fieldValue.FieldValue, out dateTime))
                                //    {
                                //        jsonWriter.WriteValue(dateTime);
                                //    }

                                //}
                                //else
                                //{
								if (field.ControlType == "CheckBox" && field.CheckBoxReturnBool == true)
								{
                                    jsonWriter.WriteValue(Convert.ToBoolean(fieldValue.FieldValue));
								}
								else
								{
									jsonWriter.WriteValue(fieldValue.FieldValue);
								}
								//}

							}
                        }
                    }

                    if (!fieldValueFound)
                    {
						if (WebUser.IsAdminOrContentAdminOrContentPublisherOrContentAuthor || WebUser.IsInRoles(field.ViewRoles))
						{
							content.Replace(field.Token, field.DefaultValue);
							if (Config.RenderJSONOfData)
							{
								jsonWriter.WritePropertyName(field.Name);
								if (field.ControlType == "CheckBox" && field.CheckBoxReturnBool == true)
								{
									jsonWriter.WriteValue(Convert.ToBoolean(field.DefaultValue));
								}
								else
								{
									jsonWriter.WriteValue(field.DefaultValue);
								}
							}
						}
						else
						{
							content.Replace(field.Token, string.Empty);
						}
						
                    }
                }

                if (Config.RenderJSONOfData)
                {
                    //if (config.IsGlobalView)
                    //{
                    //    jsonWriter.WriteEndObject();
                    //}
                    jsonWriter.WriteEndObject();
                }

                content.Replace("$_EditLink_$", itemEditLink);
                content.Replace("$_ItemID_$", item.ItemID.ToString());
                content.Replace("$_SortOrder_$", item.SortOrder.ToString());

                if (!String.IsNullOrWhiteSpace(content))
                {
                    itemsMarkup.Add(content);
                }
                
            }
            if (Config.DescendingSort)
            {
                itemsMarkup.Sort(delegate (IndexedStringBuilder a, IndexedStringBuilder b)
                {
                    int xdiff = b.SortOrder1.CompareTo(a.SortOrder1);
                    if (xdiff != 0) return xdiff;
                    else return b.SortOrder2.CompareTo(a.SortOrder2);
                });
            }
            else
            {
                itemsMarkup.Sort(delegate (IndexedStringBuilder a, IndexedStringBuilder b)
                {
                    int xdiff = a.SortOrder1.CompareTo(b.SortOrder1);
                    if (xdiff != 0) return xdiff;
                    else return a.SortOrder2.CompareTo(b.SortOrder2);
                });
            }
            StringBuilder allItems = new StringBuilder();
            if (displaySettings.ItemsPerGroup == -1)
            {
                foreach (IndexedStringBuilder sb in itemsMarkup)
                {
                        //allItems.Append(displaySettings.GlobalViewModuleGroupMarkup.Replace("$_ModuleGroupName_$", sb.GroupName));
                    allItems.Append(sb.ToString());
                }
                if (usingGlobalViewMarkup)
                {

                    strOutput.AppendFormat(displaySettings.ItemsWrapperFormat, displaySettings.GlobalViewMarkup.Replace("$_ModuleGroups_$", allItems.ToString()));
                }
                else
                {
                    strOutput.AppendFormat(displaySettings.ItemsWrapperFormat, allItems.ToString());
                }
                
            }
            else
            {
                int itemIndex = 0;

                decimal totalGroupCount = Math.Ceiling(itemsMarkup.Count / Convert.ToDecimal(displaySettings.ItemsPerGroup));

                if (totalGroupCount < 1 && itemsMarkup.Count > 0)
                {
                    totalGroupCount = 1;
                }

                int currentGroup = 1;
                List<StringBuilder> groups = new List<StringBuilder>();
                while (currentGroup <= totalGroupCount && itemIndex < itemsMarkup.Count)
                {
                    StringBuilder group = new StringBuilder();
                    group.Append(displaySettings.ItemsRepeaterMarkup);
                    //group.SortOrder1 = itemsMarkup[itemIndex].SortOrder1;
                    //group.SortOrder2 = itemsMarkup[itemIndex].SortOrder2;
                    //group.GroupName = itemsMarkup[itemIndex].GroupName;
                    for (int i = 0; i < displaySettings.ItemsPerGroup; i++)
                    {
                        if (itemIndex < itemsMarkup.Count)
                        {
                            group.Replace("$_Items[" + i.ToString() + "]_$", itemsMarkup[itemIndex].ToString());
                            itemIndex++;
                        }
                        else
                        {
                            break;
                        }
                        
                    }
                    groups.Add(group);
                    currentGroup++;
                }

                //groups.Sort(delegate (IndexedStringBuilder a, IndexedStringBuilder b) {
                //    int xdiff = a.SortOrder1.CompareTo(b.SortOrder1);
                //    if (xdiff != 0) return xdiff;
                //    else return a.SortOrder2.CompareTo(b.SortOrder2);
                //});

                foreach (StringBuilder group in groups)
                {
                    allItems.Append(group.ToString());
                }

                strOutput.AppendFormat(displaySettings.ItemsWrapperFormat, Regex.Replace(allItems.ToString(), @"(\$_Items\[[0-9]+\]_\$)", string.Empty, RegexOptions.Multiline));
            }

            

            //strOutput.Append(displaySettings.ItemListMarkupBottom);
            if (Config.RenderJSONOfData)
            {
                if (Config.JsonLabelObjects || Config.IsGlobalView)
                {
                    jsonWriter.WriteEndObject();

                    if (Config.IsGlobalView)
                    {
                        jsonWriter.WriteEndObject();
                        jsonWriter.WriteEnd();
                    }
                }
                else
                {
                    jsonWriter.WriteEndArray();
                }

                MarkupScript jsonScript = new MarkupScript();
                
                jsonWriter.Close();
                stringWriter.Close();

                jsonScript.RawScript = stringWriter.ToString();
                jsonScript.Position = Config.JsonRenderLocation;
                jsonScript.ScriptName = "sflexi" + module.ModuleId.ToString() + Config.MarkupDefinitionName.ToCleanFileName() + "-JSON";

                List<MarkupScript> scripts = new List<MarkupScript>();
                scripts.Add(jsonScript);

                SuperFlexiHelpers.SetupScripts(scripts, Config, displaySettings, siteSettings.UseSslOnAllPages, IsEditable, IsPostBack, ClientID, ModuleId, PageId, Page, this);
            }

            if (Config.UseFooter && Config.FooterLocation == "InnerBodyPanel" && !String.IsNullOrWhiteSpace(Config.FooterContent) && !String.Equals(Config.FooterContent, "<p>&nbsp;</p>")) 
            {
                try
                {
                    strOutput.AppendFormat(displaySettings.FooterContentFormat, Config.FooterContent);
                }
                catch (System.FormatException ex)
                {
                    log.ErrorFormat(markupErrorFormat, "FooterContentFormat", moduleTitle, ex);
                }
            }
            
            strOutput.Append(markupBottom);

            SuperFlexiHelpers.ReplaceStaticTokens(strOutput, Config, IsEditable, displaySettings, module.ModuleId, pageSettings, siteSettings, out strOutput);
            
            //this is for displaying all of the selected values from the items outside of the items themselves
            foreach (CheckBoxListMarkup cblm in Config.CheckBoxListMarkups)
            {
                StringBuilder cblmContent = new StringBuilder();

                if (fields.Count > 0 && cblm.SelectedValues.Count > 0)
                {
                    Field theField = fields.Where(field => field.Name == cblm.Field).Single();
                    if (theField != null)
                    {
                        List<CheckBoxListMarkup.SelectedValue> distinctSelectedValues = new List<CheckBoxListMarkup.SelectedValue>();
                        foreach (CheckBoxListMarkup.SelectedValue selectedValue in cblm.SelectedValues)
                        {
                            CheckBoxListMarkup.SelectedValue match = distinctSelectedValues.Find(i => i.Value == selectedValue.Value);
                            if (match == null)
                            {
                                distinctSelectedValues.Add(selectedValue);
                            }
                            else
                            {
                                match.Count++;
                            }
                        }
                        //var selectedValues = cblm.SelectedValues.GroupBy(selectedValue => selectedValue.Value)
                        //    .Select(distinctSelectedValue => new { Value = distinctSelectedValue.Key, Count = distinctSelectedValue.Count(), ItemID = distinctSelectedValue.ItemID })
                        //    .OrderBy(x => x.Value);
                        foreach (CheckBoxListMarkup.SelectedValue value in distinctSelectedValues)
                        {
                            cblmContent.Append(cblm.Markup.Replace(theField.Token, value.Value).Replace("$_ValueItemID_$", value.ItemID.ToString()) + cblm.Separator);
                            cblmContent.Replace("$_CBLValueCount_$", value.Count.ToString());
                        }
                    }

                    if (cblmContent.Length >= cblm.Separator.Length)
                    {
                        cblmContent.Length -= cblm.Separator.Length;
                    }

                    strOutput.Replace(cblm.Token, cblmContent.ToString());
                }

                strOutput.Replace(cblm.Token, string.Empty);
            }
            theLit.Text = strOutput.ToString();
        }

        private bool IsCheckBoxListField(Field field)
        {
            if (field.ControlType == "CheckBoxList" || field.ControlType == "DynamicCheckBoxList")
            {
                return true;
            }
            return false;
        }

        private bool IsRadioButtonListField(Field field)
        {
            if (field.ControlType == "RadioButtonList" || field.ControlType == "DynamicRadioButtonList")
            {
                return true;
            }
            return false;
        }

        private bool IsDateField(Field field)
        {
            if (field.ControlType == "DateTime" || field.ControlType == "Date" || (field.ControlType == "TextBox" && (field.TextBoxMode == "Date" || field.TextBoxMode == "DateTime" || field.TextBoxMode == "DateTimeLocal")))
            {
                return true;
            }
            return false;
        }

        void link_ContentChanged(object sender, ContentChangedEventArgs e)
        {
            IndexBuilderProvider indexBuilder = IndexBuilderManager.Providers["SuperFlexiIndexBuilderProvider"];
            if (indexBuilder != null)
            {
                indexBuilder.ContentChangedHandler(sender, e);
            }
        }

        protected string FormatEditUrl(int itemId)
        {
            return SiteRoot + "/SuperFlexi/Edit.aspx?ItemID=" + itemId.ToInvariantString()
                + "&mid=" + ModuleId.ToInvariantString()
                + "&pageid=" + PageId.ToInvariantString();
        }

        #region OnInit

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            this.Load += new EventHandler(Page_Load);
        }
        #endregion
    }

    public class IndexedStringBuilder
    {
        private StringBuilder _stringBuilder;
        public string CurrentString => _stringBuilder.ToString();
        public int Length => _stringBuilder.Length;

        private int sortOrder1 = 0;
        public int SortOrder1 { get { return sortOrder1; } set { sortOrder1 = value; } }

        private int sortOrder2 = 0;
        public int SortOrder2 { get { return sortOrder2; } set { sortOrder2 = value; } }

        private string groupName = string.Empty;
        public string GroupName { get { return groupName; } set { groupName = value; } }
        public IndexedStringBuilder()
        {
            _stringBuilder = new StringBuilder();
        }
        public IndexedStringBuilder Append(string s)
        {
            _stringBuilder.Append(s);
            return this;
        }
        public IndexedStringBuilder Append(char c)
        {
            _stringBuilder.Append(c);
            return this;
        }

        public IndexedStringBuilder Append(object o)
        {
            _stringBuilder.Append(o);
            return this;
        }

        public IndexedStringBuilder Replace(string oldValue, string newValue)
        {
            _stringBuilder.Replace(oldValue, newValue);
            return this;
        }

        public IndexedStringBuilder Replace(char oldChar, char newChar)
        {
            _stringBuilder.Replace(oldChar, newChar);
            return this;
        }

        public IndexedStringBuilder Replace(string oldValue, string newValue, int startIndex, int count)
        {
            _stringBuilder.Replace(oldValue, newValue, startIndex, count);
            return this;
        }

        public IndexedStringBuilder Replace(char oldChar, char newChar, int startIndex, int count)
        {
            _stringBuilder.Replace(oldChar, newChar, startIndex, count);
            return this;
        }
        public static IndexedStringBuilder operator +(IndexedStringBuilder sb, string s) => sb.Append(s);

        public static IndexedStringBuilder operator +(IndexedStringBuilder sb, char c) => sb.Append(c);

        public static IndexedStringBuilder operator +(IndexedStringBuilder sb, object o) => sb.Append(o);

        public static implicit operator string (IndexedStringBuilder sb) => sb.CurrentString;

        public override string ToString() => CurrentString;

        public string ToString(int startIndex, int length) => _stringBuilder.ToString(startIndex, length);

    }
}