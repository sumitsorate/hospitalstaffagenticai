

-- Demo Steps :
-- Clean Data
	Delete From PlannedShift
	Delete From LeaveRequests
	Delete From NurseAvailability
	Delete From AgentConversations

	Select * From NurseAvailability
-- Insert Mock Data


 
-- Night Shift ICU- Olivia(ICU)
INSERT INTO PlannedShift
(shift_date,shift_type_id,department_id,slot_number,shift_status_id,assigned_staff_id)
VALUES('2025-08-20', 1,1,1,1,2)

-- Night Shift ICU- Olivia(ICU)
INSERT INTO PlannedShift
(shift_date,shift_type_id,department_id,slot_number,shift_status_id,assigned_staff_id)
VALUES('2025-08-21', 1,1,1,1,2)

-- Night Shift ICU- Olivia(ICU)
INSERT INTO PlannedShift
(shift_date,shift_type_id,department_id,slot_number,shift_status_id,assigned_staff_id)
VALUES('2025-08-22', 1,1,1,1,2)

-- Night Shift ICU- Olivia(ICU)
INSERT INTO PlannedShift
(shift_date,shift_type_id,department_id,slot_number,shift_status_id,assigned_staff_id)
VALUES('2025-08-23', 1,1,2,1,2)

-- Night Shift ICU- Olivia(ICU)
INSERT INTO PlannedShift
(shift_date,shift_type_id,department_id,slot_number,shift_status_id,assigned_staff_id)
VALUES('2025-08-24', 1,1,2,1,2)

-- Night Shift ICU- Olivia(ICU)
INSERT INTO PlannedShift
(shift_date,shift_type_id,department_id,slot_number,shift_status_id,assigned_staff_id)
VALUES('2025-08-25', 1,1,2,1,2)


-- Night Shift ICU- Olivia(ICU)
INSERT INTO PlannedShift
(shift_date,shift_type_id,department_id,slot_number,shift_status_id,assigned_staff_id)
VALUES('2025-08-26', 1,1,2,1,2)


-- Morning Shift General- Emma(General)
INSERT INTO PlannedShift
(shift_date,shift_type_id,department_id,slot_number,shift_status_id,assigned_staff_id)
VALUES('2025-08-20', 2,3,1,1,4)

-- Morning Shift General- Emma(General)
INSERT INTO PlannedShift
(shift_date,shift_type_id,department_id,slot_number,shift_status_id,assigned_staff_id)
VALUES('2025-08-21', 2,3,1,1,4)

-- Morning Shift General- Emma(General)
INSERT INTO PlannedShift
(shift_date,shift_type_id,department_id,slot_number,shift_status_id,assigned_staff_id)
VALUES('2025-08-22', 2,3,1,1,4)

-- Morning Shift General- Emma(General)
INSERT INTO PlannedShift
(shift_date,shift_type_id,department_id,slot_number,shift_status_id,assigned_staff_id)
VALUES('2025-08-23', 2,3,1,1,4)

-- Morning Shift General- Emma(General)
INSERT INTO PlannedShift
(shift_date,shift_type_id,department_id,slot_number,shift_status_id,assigned_staff_id)
VALUES('2025-08-24',2,3,1,1,4)

-- Morning Shift General- Emma(General)
INSERT INTO PlannedShift
(shift_date,shift_type_id,department_id,slot_number,shift_status_id,assigned_staff_id)
VALUES('2025-08-25', 2,3,1,1,4)

-- Morning Shift General- Emma(General)
INSERT INTO PlannedShift
(shift_date,shift_type_id,department_id,slot_number,shift_status_id,assigned_staff_id)
VALUES('2025-08-26',2,3,1,1,4)


-- Evening Shift Pediatrics- Liam 
INSERT INTO PlannedShift
(shift_date,shift_type_id,department_id,slot_number,shift_status_id,assigned_staff_id)
VALUES('2025-08-20', 3,2,1,1,3)

-- Evening Shift Pediatrics- Liam 
INSERT INTO PlannedShift
(shift_date,shift_type_id,department_id,slot_number,shift_status_id,assigned_staff_id)
VALUES('2025-08-21', 3,2,1,1,3)

-- Evening Shift Pediatrics- Liam 
INSERT INTO PlannedShift
(shift_date,shift_type_id,department_id,slot_number,shift_status_id,assigned_staff_id)
VALUES('2025-08-22', 3,2,1,1,3)

-- Evening Shift Pediatrics- Liam 
INSERT INTO PlannedShift
(shift_date,shift_type_id,department_id,slot_number,shift_status_id,assigned_staff_id)
VALUES('2025-08-23', 3,2,3,1,3)

-- Evening Shift Pediatrics- Liam 
INSERT INTO PlannedShift
(shift_date,shift_type_id,department_id,slot_number,shift_status_id,assigned_staff_id)
VALUES('2025-08-24', 3,2,3,1,3)

-- Evening Shift Pediatrics- Liam 
INSERT INTO PlannedShift
(shift_date,shift_type_id,department_id,slot_number,shift_status_id,assigned_staff_id)
VALUES('2025-08-25', 3,2,3,1,3)

-- Evening Shift Pediatrics- Liam 
INSERT INTO PlannedShift
(shift_date,shift_type_id,department_id,slot_number,shift_status_id,assigned_staff_id)
VALUES('2025-08-26', 3,2,3,1,3)



-- Unassigned on of the Shift - Workflow
Select * From PlannedShift Where shift_date='2025-08-25'

Update PlannedShift Set assigned_staff_id=null , shift_status_id=5
Where planned_shift_id=288

 -- Step 1: Run the uncovered shift flow
 -- Assign it to Ava
 -- Step 2 : Add a leave request for Ava : Add a sick leave for Ava on 25th Aug
 -- Assign it to noah--if in output
 -- Step 3:  How system behaves when staff is not available
 --  Make the same 25th aug ICU night shift as Vacant
 --  Add unavailability for staff on 25th day 
 --  Run the chat iteratively


-- add a  unavailability - 
Select * From Staff

-- Fatigue Rules Check

-- Elena not available : morning, evening, night
Insert into NurseAvailability values
(1,'2025-08-25',0,1,'')
Insert into NurseAvailability values
(1,'2025-08-25',0,2,'')
Insert into NurseAvailability values
(1,'2025-08-25',0,3,'')

-- Olivia not available : morning, evening, night
Insert into NurseAvailability values
(2,'2025-08-25',0,1,'')
Insert into NurseAvailability values
(2,'2025-08-25',0,2,'')
Insert into NurseAvailability values
(2,'2025-08-25',0,3,'')

-- Liam not available : morning, evening, night
Insert into NurseAvailability values
(3,'2025-08-25',0,1,'')
Insert into NurseAvailability values
(3,'2025-08-25',0,2,'')
Insert into NurseAvailability values
(3,'2025-08-25',0,3,'')

-- Emma not available : morning, evening, night
Insert into NurseAvailability values
(4,'2025-08-25',0,1,'')
Insert into NurseAvailability values
(4,'2025-08-25',0,2,'')
Insert into NurseAvailability values
(4,'2025-08-25',0,3,'')

-- Noah not available : morning, evening, night
Insert into NurseAvailability values
(5,'2025-08-25',0,1,'')
Insert into NurseAvailability values
(5,'2025-08-25',0,2,'')
Insert into NurseAvailability values
(5,'2025-08-25',0,3,'')


-- Ava not available : morning, evening, night
Insert into NurseAvailability values
(6,'2025-08-25',0,1,'')
Insert into NurseAvailability values
(6,'2025-08-25',0,2,'')
Insert into NurseAvailability values
(6,'2025-08-25',0,3,'')

-- Mia not available : morning, evening, night
Insert into NurseAvailability values
(7,'2025-08-25',0,1,'')
Insert into NurseAvailability values
(7,'2025-08-25',0,2,'')
Insert into NurseAvailability values
(7,'2025-08-25',0,3,'')

-- Sumit not available : morning, evening, night
Insert into NurseAvailability values
(15,'2025-08-25',0,1,'')
Insert into NurseAvailability values
(15,'2025-08-25',0,2,'')
Insert into NurseAvailability values
(15,'2025-08-25',0,3,'')

Delete From NurseAvailability

Select * From ShiftType

