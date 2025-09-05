class Patient:
    def __init__(self, id, name, age, disease):
        self.id = id
        self.name = name
        self.age = age
        self.disease = disease
        self.is_admitted = False

class Doctor:
    def __init__(self, id, name, speciality):
        self.id = id
        self.name = name
        self.speciality = speciality

class Hospital:
    def __init__(self):
        self.patients = []
        self.doctors = []

    def add_patient(self, id, name, age, disease):
        patient = Patient(id, name, age, disease)
        self.patients.append(patient)
        print(f"Patient {name} added successfully")

    def add_doctor(self, id, name, speciality):
        doctor = Doctor(id, name, speciality)
        self.doctors.append(doctors)
        print("Doctor added")

    def admit_patient(self, id):
        for patient in self.patients:
            if patient.id == id:
                if patient.is_admitted == True:
                    print("Patient already admitted")
                else:
                    patient.is_admitted = True
                    print(f"Patient {patient.name} admitted successfully")
                return
        print("No such patient found")

    def discharge_patient(self, id):
        for pat in self.patients:
            if pat.id = id:
                if pat.is_admitted == False:
                    print("Patient not admitted yet")
                else:
                    pat.is_admitted = False
                    print(f"Patient {pat.name} discharged successfully")
                return
        print("Patient not found")

    def list_patients(self):
        for p in self.patients:
            print(f"{p.id}: {p.name}, Age: {p.age}, Disease: {p.disease}, Admitted: {p.is_admitted}")

    def list_doctors(self):
        for doc in self.doctors:
            print(f"{doc.id}: {doc.name} - {doc.speciality}")


def main():
    h = Hospital()

    while True:
        print("\n1. Add Patient")
        print("2. Add Doctor")
        print("3. Admit Patient")
        print("4. Discharge Patient")
        print("5. List Patients")
        print("6. List Doctors")
        print("7. Exit")

        choice = int(input("Enter your choice: "))

        if choice == 1:
            id = input("Enter Patient ID: ")
            name = input("Enter Patient Name: ")
            age = input("Enter Patient Age: ")
            disease = input("Enter Disease: ")
            h.add_patient(id, name, age, disease)

        elif choice == 2:
            id = input("Enter Doctor ID: ")
            name = input("Enter Doctor Name: ")
            speciality = input("Enter Speciality: ")
            h.add_doctor(id, name, speciality)

        elif choice == 3:
            id = input("Enter Patient ID to admit: ")
            h.admit_patient(id)

        elif choice == 4:
            id = input("Enter Patient ID to discharge: ")
            h.discharge_patient(id)

        elif choice == 5:
            h.list_patients()

        elif choice == 6:
            h.list_doctors()

        elif choice == 7:
            print("Exiting...")
            break

        else:
            print("Invalid choice")

if __name__ == "__main__":
    main()
