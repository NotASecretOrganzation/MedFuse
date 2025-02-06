from crewai import Agent, Task, Crew, Process
from textwrap import dedent

# Define patient-centered research agents
patient_advocate = Agent(
    role='Patient Advocate',
    goal='Ensure patient wellbeing and represent patient interests throughout the research process',
    backstory="""You are a dedicated patient advocate with extensive experience in 
    oncology care. You ensure that research practices prioritize patient dignity, 
    comfort, and individual needs while maintaining clear communication with patients 
    and their families.""",
    verbose=True,
    allow_delegation=True
)

care_coordinator = Agent(
    role='Care Coordinator',
    goal='Coordinate personalized care plans and support services',
    backstory="""You are an experienced healthcare coordinator who specializes in 
    creating comprehensive support systems for cancer patients. You ensure each 
    patient receives personalized attention and has access to all necessary 
    resources.""",
    verbose=True,
    allow_delegation=True
)

oncologist = Agent(
    role='Clinical Oncologist',
    goal='Provide personalized treatment while advancing research',
    backstory="""You are a compassionate oncologist who believes in treating the 
    person, not just the disease. You focus on developing personalized treatment 
    approaches while ensuring patients fully understand and consent to any research 
    participation.""",
    verbose=True,
    allow_delegation=True
)

research_ethicist = Agent(
    role='Research Ethicist',
    goal='Ensure ethical research practices and patient rights protection',
    backstory="""You are an ethics specialist focused on maintaining the highest 
    standards of patient dignity and autonomy in cancer research. You ensure all 
    research activities prioritize patient wellbeing.""",
    verbose=True,
    allow_delegation=True
)

# Define patient-centered tasks
patient_support_task = Task(
    description="""Create and implement comprehensive support plans for each patient, 
    including emotional support, educational resources, and family assistance""",
    agent=patient_advocate
)

care_coordination_task = Task(
    description="""Develop and manage personalized care plans that integrate 
    treatment, support services, and research participation in a way that 
    prioritizes patient comfort and preferences""",
    agent=care_coordinator
)

treatment_planning_task = Task(
    description="""Design and implement personalized treatment approaches that 
    respect patient choices and incorporate research opportunities only when 
    appropriate and beneficial to the patient""",
    agent=oncologist
)

ethics_review_task = Task(
    description="""Review all research protocols and practices to ensure they 
    maintain patient dignity, autonomy, and wellbeing while advancing medical 
    knowledge""",
    agent=research_ethicist
)

# Create the patient-centered research crew
patient_centered_crew = Crew(
    agents=[patient_advocate, care_coordinator, oncologist, research_ethicist],
    tasks=[patient_support_task, care_coordination_task, treatment_planning_task, ethics_review_task],
    process=Process.sequential,
    verbose=2
)

class PatientCenteredCare:
    def __init__(self):
        self.crew = patient_centered_crew
        
    def create_care_plan(self, patient_preferences):
        """Create a personalized care plan based on patient preferences"""
        return {
            "treatment_plan": self._design_treatment_plan(patient_preferences),
            "support_services": self._arrange_support_services(patient_preferences),
            "research_participation": self._evaluate_research_options(patient_preferences)
        }
    
    def _design_treatment_plan(self, preferences):
        """Design treatment plan prioritizing patient preferences"""
        plan = {
            "primary_treatment": preferences.get("preferred_treatment_approach"),
            "alternative_options": self._identify_alternatives(preferences),
            "comfort_measures": preferences.get("comfort_priorities"),
            "schedule_flexibility": preferences.get("scheduling_needs")
        }
        return plan
    
    def _arrange_support_services(self, preferences):
        """Arrange comprehensive support services"""
        services = {
            "emotional_support": self._setup_counseling(preferences),
            "family_support": self._arrange_family_services(preferences),
            "practical_assistance": self._organize_practical_help(preferences),
            "educational_resources": self._provide_education(preferences)
        }
        return services
    
    def _evaluate_research_options(self, preferences):
        """Evaluate research opportunities that align with patient interests"""
        if not preferences.get("interested_in_research", False):
            return {"participation": "None - patient preference"}
        
        return {
            "suitable_studies": self._find_matching_studies(preferences),
            "patient_benefits": self._identify_direct_benefits(),
            "opt_out_protocol": "Available at any time",
            "communication_plan": self._create_communication_plan()
        }
    
    def get_patient_feedback(self):
        """Collect and process patient feedback to improve care"""
        feedback_system = {
            "regular_check_ins": "Weekly",
            "comfort_assessment": "Daily",
            "satisfaction_survey": "Monthly",
            "suggestion_box": "Always available",
            "family_feedback": "Bi-weekly"
        }
        return feedback_system
    
    def adjust_care_plan(self, feedback):
        """Modify care plan based on patient feedback"""
        adjustments = {
            "treatment_modifications": self._process_treatment_feedback(feedback),
            "support_adjustments": self._modify_support_services(feedback),
            "communication_updates": self._update_communication_approach(feedback)
        }
        return adjustments

def run_patient_centered_care():
    """Initialize and run the patient-centered care system"""
    try:
        care_system = PatientCenteredCare()
        return care_system
    except Exception as e:
        print(f"Error initializing care system: {str(e)}")
        return None

if __name__ == "__main__":
    care_system = run_patient_centered_care()
    if care_system:
        # Example patient preferences
        patient_preferences = {
            "preferred_treatment_approach": "Minimally invasive",
            "comfort_priorities": ["Pain management", "Home care"],
            "scheduling_needs": "Afternoon appointments",
            "interested_in_research": True
        }
        
        # Create and implement care plan
        care_plan = care_system.create_care_plan(patient_preferences)
        print("Patient-Centered Care Plan Created")
