from tinytroupe import TaskExecutor, Analyst, TroupeDirector, Task
from typing import List, Dict
import asyncio

class DentalCareTeam:
    def __init__(self):
        self.director = TroupeDirector()
        
        # Define specialized dental care roles
        self.team = {
            "general_dentist": TaskExecutor(
                name="General Dentist",
                expertise=["general_dentistry", "diagnosis", "treatment_planning"],
                description="Provides primary dental care and coordinates treatments"
            ),
            
            "dental_hygienist": TaskExecutor(
                name="Dental Hygienist",
                expertise=["cleaning", "preventive_care", "patient_education"],
                description="Performs cleanings and preventive care"
            ),
            
            "treatment_coordinator": TaskExecutor(
                name="Treatment Coordinator",
                expertise=["treatment_planning", "scheduling", "insurance_coordination"],
                description="Manages treatment plans and scheduling"
            ),
            
            "dental_specialist": TaskExecutor(
                name="Dental Specialist",
                expertise=["endodontics", "orthodontics", "oral_surgery"],
                description="Provides specialized dental treatments"
            ),
            
            "patient_care_coordinator": TaskExecutor(
                name="Patient Care Coordinator",
                expertise=["patient_communication", "care_planning", "follow_up"],
                description="Ensures patient comfort and treatment adherence"
            )
        }
        
        # Register team members
        for role, member in self.team.items():
            self.director.register_agent(member)

    async def initial_examination(self, patient_data: Dict) -> Task:
        """Perform initial dental examination and create treatment plan"""
        exam_task = Task(
            name="Initial Examination",
            description="Complete dental examination and treatment planning",
            required_expertise=["general_dentistry", "diagnosis"],
            parameters={
                "patient_data": patient_data,
                "examination_type": "comprehensive"
            }
        )
        
        await self.director.assign_task(
            task=exam_task,
            executor=self.team["general_dentist"]
        )
        return exam_task

    async def schedule_hygiene_care(self, patient_id: str, care_type: str) -> Task:
        """Schedule and perform dental hygiene services"""
        hygiene_task = Task(
            name="Dental Hygiene",
            description="Provide dental cleaning and preventive care",
            required_expertise=["cleaning", "preventive_care"],
            parameters={
                "patient_id": patient_id,
                "care_type": care_type
            }
        )
        
        await self.director.assign_task(
            task=hygiene_task,
            executor=self.team["dental_hygienist"]
        )
        return hygiene_task

    async def coordinate_treatment(self, treatment_plan: Dict) -> Task:
        """Coordinate treatment schedule and insurance"""
        coordination_task = Task(
            name="Treatment Coordination",
            description="Coordinate treatment schedule and insurance coverage",
            required_expertise=["treatment_planning", "insurance_coordination"],
            parameters=treatment_plan
        )
        
        await self.director.assign_task(
            task=coordination_task,
            executor=self.team["treatment_coordinator"]
        )
        return coordination_task

    async def specialist_consultation(self, case_details: Dict) -> Task:
        """Arrange specialist consultation and treatment"""
        specialist_task = Task(
            name="Specialist Consultation",
            description="Provide specialized dental treatment",
            required_expertise=["endodontics", "orthodontics", "oral_surgery"],
            parameters=case_details
        )
        
        await self.director.assign_task(
            task=specialist_task,
            executor=self.team["dental_specialist"]
        )
        return specialist_task

    async def manage_patient_care(self, patient_info: Dict) -> Task:
        """Manage ongoing patient care and communication"""
        care_task = Task(
            name="Patient Care Management",
            description="Ensure patient comfort and treatment follow-up",
            required_expertise=["patient_communication", "care_planning"],
            parameters=patient_info
        )
        
        await self.director.assign_task(
            task=care_task,
            executor=self.team["patient_care_coordinator"]
        )
        return care_task

    async def create_treatment_plan(self, examination_results: Dict) -> Dict:
        """Create comprehensive treatment plan"""
        return {
            "preventive_care": {
                "cleaning_frequency": "6 months",
                "x_rays": "yearly",
                "fluoride": "as needed"
            },
            "restorative_care": examination_results.get("restorative_needs", []),
            "specialist_referrals": examination_results.get("referral_needs", []),
            "estimated_timeline": "12 months",
            "priority_procedures": examination_results.get("priority_treatments", [])
        }

    async def run_dental_care_workflow(self, patient_data: Dict):
        """Execute complete dental care workflow"""
        try:
            # Initial examination
            exam_task = await self.initial_examination(patient_data)
            exam_results = await exam_task.complete()

            # Create treatment plan
            treatment_plan = await self.create_treatment_plan(exam_results)

            # Schedule immediate hygiene care if needed
            if treatment_plan["preventive_care"].get("immediate_cleaning"):
                hygiene_task = await self.schedule_hygiene_care(
                    patient_data["id"],
                    "comprehensive_cleaning"
                )

            # Coordinate overall treatment
            coordination_task = await self.coordinate_treatment(treatment_plan)

            # Setup specialist consultations if needed
            specialist_tasks = []
            for referral in treatment_plan["specialist_referrals"]:
                specialist_task = await self.specialist_consultation(referral)
                specialist_tasks.append(specialist_task)

            # Ongoing patient care management
            care_task = await self.manage_patient_care(patient_data)

            # Wait for all tasks to complete
            tasks = [coordination_task, care_task] + specialist_tasks
            await asyncio.gather(*tasks)

            return {
                "examination": exam_results,
                "treatment_plan": treatment_plan,
                "coordination_status": coordination_task.results,
                "specialist_consultations": [task.results for task in specialist_tasks],
                "patient_care_status": care_task.results
            }

        except Exception as e:
            print(f"Error in dental care workflow: {str(e)}")
            return None

# Example usage
async def main():
    # Create dental care team
    dental_team = DentalCareTeam()
    
    # Example patient data
    patient_data = {
        "id": "P12345",
        "name": "John Doe",
        "age": 35,
        "last_visit": "2024-01-15",
        "medical_history": {
            "conditions": [],
            "medications": [],
            "allergies": []
        },
        "insurance": {
            "provider": "Delta Dental",
            "plan_type": "PPO"
        }
    }
    
    # Run dental care workflow
    results = await dental_team.run_dental_care_workflow(patient_data)
    
    if results:
        print("Dental care workflow completed successfully")
        print("Results:", results)
    else:
        print("Dental care workflow encountered an error")

if __name__ == "__main__":
    asyncio.run(main())
