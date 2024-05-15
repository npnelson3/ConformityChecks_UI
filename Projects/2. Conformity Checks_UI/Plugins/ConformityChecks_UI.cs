using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Windows.Input;

[assembly: AssemblyVersion("1.0.0.1")]

namespace VMS.TPS
{

  /// <summary>
  /// A simple script to calculate R100 and R50
  /// </summary>
  public class Script
  {
    public Script()
    {
    }

        // initializeprivate variables
        private Patient _patient;
        private PlanSetup _plan;
        private StructureSet _structureSet;
        private Structure _selectedTargetStructure;
        private Structure _selectedStructureOfInterest; // similar to ROI/VOI
        private double _r50;
        private double _r100;

        private List<string> _ptvIds;
        private List<string> _structuresOfInterestIds;

        private ComboBox _ptvsComboBox;
        private ComboBox _structuresOfInterestComboBox;
        private ComboBox _calculationTypeOptionsComboBox;
        private List<string> _options;
        private Button _calculateConformityStatisticsButton;
        private TextBlock _resultsTextBlock;

        [MethodImpl(MethodImplOptions.NoInlining)]
    public void Execute(ScriptContext context , System.Windows.Window window/*, ScriptEnvironment environment*/)
    {
            // Validate patient in current context
            ValidatePatient(context);
            // Validate plan and dose in current context
            ValidatePlanAndPlanDose(context);

            try
            {
                // Patient in the current context
                _patient = GetPatient(context);

                // gets plan setup
                _plan = GetPlanSetup(context);

                //MessageBox.Show(string.Format("Target structure is called: {0}", plan.TargetVolumeID));

                // structure set (current plan)
                _structureSet = GetStructureSet(_plan);

                // get the list of ptv ids
                _ptvIds = GetPTVIds(_structureSet);
                // make sure there's at least one
                ValidatePTVList(_ptvIds);
                // get the body and other possible structures of interest
                _structuresOfInterestIds = GetStructureOfInterestIds(_structureSet);
                // make sure there's at least one structure of interest (there should always be a body/external when there's dose)
                ValidateStructureOfInterestList(_structuresOfInterestIds);

                #region Shorter example
                // ALL IN ONE VERSION calculate and display stats to user

                //if (string.IsNullOrEmpty(context.PlanSetup.TargetVolumeID) == false)
                //{
                //    DisplayConformityStatistics(
                //        context.Patient,
                //        context.PlanSetup,
                //        CalculateR50(context.PlanSetup,
                //            context.StructureSet.Structures.FirstOrDefault(x => x.DicomType == "EXTERNAL"),
                //            context.StructureSet.Structures.FirstOrDefault(x => x.Id == context.PlanSetup.TargetVolumeID)),
                //        CalculateR100(context.PlanSetup,
                //            context.StructureSet.Structures.FirstOrDefault(x => x.DicomType == "EXTERNAL"),
                //            context.StructureSet.Structures.FirstOrDefault(x => x.Id == context.PlanSetup.TargetVolumeID)));
                //}
                //else
                //{
                //    MessageBox.Show("Please select a target volume");
                //}


                #endregion

                #region Shorter example using method extraction via the longest method
                // THIS VERSION HAS MORE DETAILS

                //CalculateAndDisplayConformityIndices(context);

                #endregion



                #region Window Elements

                // combo box for ptvs
                _ptvsComboBox = new ComboBox
                {
                    // set the items source as structures that start with ptv
                    ItemsSource = _ptvIds,
                    // set the selected item as the plan target volume id OR the frist target in the list
                    SelectedItem = string.IsNullOrEmpty(_plan.TargetVolumeID) ? _ptvIds.First() : _plan.TargetVolumeID,
                    Width = 125
                };


                // combo box for structures of interest
                _structuresOfInterestComboBox = new ComboBox
                {
                    // set teh items source as structures with r50, ci, and the body
                    ItemsSource = _structuresOfInterestIds,
                    // set the selected structure of interest to the body
                    SelectedItem = _structureSet.Structures.First(x => x.DicomType == "EXTERNAL").Id,
                    Width = 125
                };

                // options for calculation
                _options = new List<string>()
                {
                    "Both",
                    "CI",
                    "R50"
                };

                // combo box for calculation selection
                _calculationTypeOptionsComboBox = new ComboBox
                {
                    // set items source as options
                    ItemsSource = _options,
                    // selected item as both
                    SelectedItem = _options.First(),
                    Width = 125
                };

                // button to calculate conformity stats
                _calculateConformityStatisticsButton = new Button
                {

                    // button content - what is says
                    Content = "Calculate conformity statistics",

                    // a little padding
                    Padding = new Thickness(10),
                    Cursor = Cursors.Hand,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Width = 260,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                _calculateConformityStatisticsButton.Click += CalculateConformityStatisticsButton_Click;

                // text block to show the results
                _resultsTextBlock = new TextBlock
                {
                    Text = "",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                #region Window Containers
                // main container
                StackPanel spMain = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                // ptv selection container and label
                StackPanel spPtvs = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                spPtvs.Children.Add(new TextBlock { 
                    Text = "Choose Target", 
                    Width = 125,
                    Margin = new Thickness(0,0,10,0)});
                spPtvs.Children.Add(_ptvsComboBox);

                // structure of interest selection container and label
                StackPanel spStructuresOfInterest = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                spStructuresOfInterest.Children.Add(new TextBlock { 
                    Text = "Choose SOI",
                    ToolTip = "Your structure of interest which contains all of the relevant isodose levels, i.e. 50% and 100%",
                    Width = 125, 
                    Margin = new Thickness(0, 0, 10, 0) });
                spStructuresOfInterest.Children.Add(_structuresOfInterestComboBox);

                // structure of interest selection container and label
                StackPanel spCalculationOptions = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                spCalculationOptions.Children.Add(new TextBlock
                {
                    Text = "Calculate",
                    Width = 125,
                    Margin = new Thickness(0, 0, 10, 0)
                });
                spCalculationOptions.Children.Add(_calculationTypeOptionsComboBox);

                // add components to the main stack panel
                spMain.Children.Add(spPtvs);
                spMain.Children.Add(spStructuresOfInterest);
                spMain.Children.Add(spCalculationOptions);
                spMain.Children.Add(_calculateConformityStatisticsButton);
                spMain.Children.Add(_resultsTextBlock);
                #endregion Containers


                #endregion Window Elements


                // window settings
                window.FontFamily = new System.Windows.Media.FontFamily("Calibri");
                window.FontSize = 14;
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                window.Content = spMain;
            }
            catch (Exception ex)
            { 
                MessageBox.Show(string.Format("Sorry, something went wrong.\n\n{0}\n\n{1}", ex.Message, ex.StackTrace));
                throw;
            }

        }
        //HELPER METHODS
        /// <summary>
        /// Gets a list of structures that begin with CI_ or R50_ or are of DicomType EXTERNAL
        /// </summary>
        /// <param name="structureSet"></param>
        /// <returns></returns>
        private List<string> GetStructureOfInterestIds(StructureSet structureSet)
        {
            return structureSet.Structures.Where(x =>
                x.Id.ToUpper().StartsWith("R50_") ||
                x.Id.ToUpper().StartsWith("CI_") ||
                x.DicomType == "EXTERNAL").Select(x => x.Id).ToList();
        }


        /// <summary>
        /// Gets a list of structures that begins with PTV (Case insensitive)
        /// </summary>
        /// <param name="structureSet"></param>
        /// <returns></returns>
        private List<string> GetPTVIds(StructureSet structureSet)
        {
            return structureSet.Structures.Where(x => x.Id.ToUpper().StartsWith("PTV")).Select(x => x.Id).ToList();
        }

        /// <summary>
        /// Validates whether the list of PTVs contains at least 1 PTV
        /// <para>Will end teh script is the list is empty</para>
        /// </summary>
        private void ValidatePTVList(List<string> ptvIds)
        {
            if (ptvIds.Count == 0)
            {
                MessageBox.Show("Sorry, it appears you don't have any structures that begin with PTV.");
                return;
            }
        }

        /// <summary>
        /// Validates whether there is a structure of DicomType EXTERNAL and/or other possible structures of interest
        /// <para>Will end teh script is the list is empty</para>
        /// </summary>
        private void ValidateStructureOfInterestList(List<string> structureOfInterestIds)
        {
            if (structureOfInterestIds.Count == 0)
            {
                MessageBox.Show("Sorry, it appears you don't have a Body (or External) or structures that begin with CI_ or R50_.");
                return;
            }
        }

        /// <summary>
        /// Initiates calculation of the conformity statistics and presents it to the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CalculateConformityStatisticsButton_Click(object sender, RoutedEventArgs e)
        {

            _selectedStructureOfInterest = GetStructure(_structuresOfInterestComboBox.SelectedItem.ToString(), _structureSet);
            _selectedTargetStructure = GetStructure(_ptvsComboBox.SelectedItem.ToString(), _structureSet);

            if (_calculationTypeOptionsComboBox.SelectedItem.ToString() == "Both")
            {
                // calculate the R50 - 50% IDL / target volume
                _r50 = Math.Round(CalculateR50(_plan, _selectedStructureOfInterest, _selectedTargetStructure),2);

                // calculate the R100 - 100% IDL / target volume
                _r100 = Math.Round(CalculateR100(_plan, _selectedStructureOfInterest, _selectedTargetStructure),2);

                // append the results
                _resultsTextBlock.Text += string.Format("{0}:\n\tR50:\t{1}\n\tCI:\t{2}\n", _selectedTargetStructure.Id, _r50, _r100);
            }
            else if (_calculationTypeOptionsComboBox.SelectedItem.ToString() == "CI")
            {
                // calculate the R100 - 100% IDL / target volume
                _r100 = Math.Round(CalculateR100(_plan, _selectedStructureOfInterest, _selectedTargetStructure),2);

                // append the results
                _resultsTextBlock.Text += string.Format("{0}:\n\tCI:\t{1}\n", _selectedTargetStructure.Id, _r100);
            }
            else
            {
                // calculate the R50 - 50% IDL / target volume
                _r50 = Math.Round(CalculateR50(_plan, _selectedStructureOfInterest, _selectedTargetStructure),2);
                // append the results
                _resultsTextBlock.Text += string.Format("{0}:\n\tR50:\t{1}\n", _selectedTargetStructure.Id, _r50);
            }

        }

        /// <summary>
        /// Calculates and displays the R50 and R100 to the user
        /// </summary>
        /// <param name="context"></param>
        private void CalculateAndDisplayConformityIndices(ScriptContext context)
        {
            // Patient in the current context
            Patient patient = GetPatient(context);

            // gets plan setup
            PlanSetup plan = GetPlanSetup(context);

            //MessageBox.Show(string.Format("Target structure is called: {0}", plan.TargetVolumeID));

            // structure set (current plan)
            StructureSet structureSet = GetStructureSet(plan);

            // the plan target volume
            Structure target = GetPlanTargetVolume(plan.TargetVolumeID, structureSet);

            // validate the target is not null
            ValidateStructureExists(target, "Please select target volume");
            // get the body structure
            Structure body = GetBodyStructure(structureSet);
            //MessageBox.Show(string.Format("EXTERNAL structure is called: {0}", body.Id));

            // calculate the R50 - 50% IDL / target volume
            double r50 = CalculateR50(plan, body, target);

            // calculate the R100 - 100% IDL / target volume
            double r100 = CalculateR100(plan, body, target);

            // calculate and display stats to user
            DisplayConformityStatistics(patient, plan, r50, r100);
        }

        /// <summary>
        /// Displays the conformity statistics from the plan to the user
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="plan"></param>
        /// <param name="r50"></param>
        /// <param name="r100"></param>
        private static void DisplayConformityStatistics(Patient patient, PlanSetup plan, double r50, double r100)
        {
            // display info to user
            MessageBox.Show(
                string.Format("Patient Id:\t\t{0}\n" +
                "Plan Id:\t\t{1}\n\n" +
                "Max Dose:\t\t{2:F1}\n\n" +
                "R100 (CI):\t\t{3:F2}\n\n" +
                "R50 (Gradient):\t{4:F2}",
                patient.Id,
                plan.Id,
                plan.Dose.DoseMax3D.ToString(),
                r100,
                r50));
        }

        /// <summary>
        /// Gets the conformity statistics result as a string
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="plan"></param>
        /// <param name="r50"></param>
        /// <param name="r100"></param>
        /// <returns>string</returns>
        private static string GetConformityStatisticsResultString(Patient patient, PlanSetup plan, double r50, double r100)
        {
         return string.Format("Patient Id:\t\t{0}\n" +
                "Plan Id:\t\t{1}\n\n" +
                "Max Dose:\t\t{2:F1}\n\n" +
                "R100 (CI):\t\t{3:F2}\n\n" +
                "R50 (Gradient):\t{4:F2}",
                patient.Id,
                plan.Id,
                plan.Dose.DoseMax3D.ToString(),
                r100,
                r50);
        }

        /// <summary>
        /// Gets the patient in the current context
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private static Patient GetPatient(ScriptContext context)
        {
            return context.Patient;
        }

        /// <summary>
        /// Calculates the ratio of the 100% isodose volume to target volume
        /// </summary>
        /// <param name="plan"></param>
        /// <param name="body"></param>
        /// <param name="targetVolume"></param>
        /// <returns></returns>
        private static double CalculateR100(PlanSetup plan, Structure structureOfInterest, Structure targetStructure)
        {
            // get the volume at 100% Rx in the structure of interest
            return plan.GetVolumeAtDose(structureOfInterest,
                    new DoseValue(100, DoseValue.DoseUnit.Percent),
                    VolumePresentation.AbsoluteCm3) / targetStructure.Volume;
        }

        /// <summary>
        /// Calculates the ratio of the 50% isodose volume to target volume
        /// </summary>
        /// <param name="plan"></param>
        /// <param name="body"></param>
        /// <param name="targetVolume"></param>
        /// <returns></returns>
        private static double CalculateR50(PlanSetup plan, Structure structureOfInterest, Structure targetStructure)
        {
            // get the volume at 50% Rx in the structure of interest
            return plan.GetVolumeAtDose(structureOfInterest,
                    new DoseValue(50, DoseValue.DoseUnit.Percent),
                    VolumePresentation.AbsoluteCm3) / targetStructure.Volume;
        }

        /// <summary>
        /// Gets the body structure (external)
        /// </summary>
        /// <param name="structureSet"></param>
        /// <returns></returns>
        private static Structure GetBodyStructure(StructureSet structureSet)
        {
            return structureSet.Structures.FirstOrDefault(x => x.DicomType == "EXTERNAL");
        }

        /// <summary>
        /// Gets the plan's target volume
        /// </summary>
        /// <param name="targetId"></param>
        /// <param name="structureSet"></param>
        /// <returns></returns>
        private static Structure GetPlanTargetVolume(string targetId, StructureSet structureSet)
        {
            return structureSet.Structures.FirstOrDefault(x => x.Id == targetId);
        }

        /// <summary>
        /// Gets the structure from teh structure set with the matching Id
        /// </summary>
        /// <param name="structureId"></param>
        /// <param name="structureSet"></param>
        /// <returns>Structure with the matching Id</returns>
        private static Structure GetStructure(string structureId, StructureSet structureSet)
        {
            return structureSet.Structures.FirstOrDefault(x => x.Id == structureId);
        }

        /// <summary>
        /// Gets the plan's structure set
        /// </summary>
        /// <param name="plan"></param>
        /// <returns></returns>
        private static StructureSet GetStructureSet(PlanSetup planSetup)
        {
            // current plan's structure set
            return planSetup.StructureSet;
        }

        /// <summary>
        /// Gets the plan setup in the current context
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private static PlanSetup GetPlanSetup(ScriptContext context)
        {
            // current selected plan
            return context.PlanSetup;
        }

        /// <summary>
        /// Validates that the current context is a Patient
        /// <para></para>Will alert the user and end the script
        /// </summary>
        /// <param name="context"></param>
        private void ValidatePatient(ScriptContext context)
        {
            if (context.Patient == null)
            {
                MessageBox.Show("Please open a patient");
                return;
            }
        }

        /// <summary>
        /// Validates the plan and dose for the current context
        /// </summary>
        /// <param name="context"></param>
        private void ValidatePlanAndPlanDose(ScriptContext context)
        {
            if (context.PlanSetup == null || context.PlanSetup.IsDoseValid == false)
            {
                MessageBox.Show("Please open a plan and ensure it has calculated dose.");
                return;
            }
        }
        /// <summary>
        /// Validates the SS is not null
        /// </summary>
        /// <param name="structureSet"></param>
        private void ValidateStructureSet(ScriptContext context)
        {
            if (context.StructureSet == null)
            {
                MessageBox.Show("Sorry, the selected plan does not have a structure set");
                return;
            }
        }
        /// <summary>
        /// Validates the provided structure is not null
        /// <para> Will alert user with the provided message and end script</para>
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="alertMessage"></param>
        private void ValidateStructureExists(Structure structure, string alertMessage)
        {
            if (structure == null)
            {
                MessageBox.Show(alertMessage);
                return;
            }
        }
    }
}
