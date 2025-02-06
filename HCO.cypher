// Node definitions with properties
CREATE (idn:IDN {
    name: 'Regional Healthcare Network',
    region: 'Northeast',
    established: '2020'
});

CREATE (hospital:Hospital {
    name: 'Central Medical Center',
    beds: 500,
    level: 'Level 1 Trauma'
});

CREATE (physicianGroup:PhysicianGroup {
    name: 'Metropolitan Physicians Alliance',
    specialty: 'Multi-specialty',
    memberCount: 150
});

CREATE (clinic:Clinic {
    name: 'Community Health Clinic',
    type: 'Primary Care',
    locations: 3
});

CREATE (surgeryCenter:SurgeryCenter {
    name: 'Advanced Surgery Center',
    operatingRooms: 8,
    accreditation: 'AAAHC'
});

CREATE (imagingCenter:ImagingCenter {
    name: 'Precision Imaging',
    modalities: ['MRI', 'CT', 'X-Ray', 'Ultrasound']
});

CREATE (gpo:GPO {
    name: 'Healthcare Supply Alliance',
    memberCount: 1200,
    annualPurchaseVolume: 5000000
});

CREATE (doctor:Doctor {
    name: 'Dr. Sarah Johnson',
    specialty: 'Cardiology',
    licenseNumber: 'MD12345'
});

// Define relationships
// IDN relationships
CREATE (idn)-[:OPERATES]->(hospital)
CREATE (idn)-[:PARTNERS_WITH]->(physicianGroup)
CREATE (idn)-[:MANAGES]->(clinic)
CREATE (idn)-[:AFFILIATES_WITH]->(surgeryCenter)
CREATE (idn)-[:CONTRACTS_WITH]->(gpo)

// Hospital relationships
CREATE (hospital)-[:EMPLOYS]->(doctor)
CREATE (hospital)-[:REFERS_TO]->(imagingCenter)
CREATE (hospital)-[:PURCHASES_FROM]->(gpo)

// Physician group relationships
CREATE (physicianGroup)-[:PRIVILEGES_AT]->(hospital)
CREATE (physicianGroup)-[:OPERATES]->(clinic)
CREATE (doctor)-[:MEMBER_OF]->(physicianGroup)

// Surgery center relationships
CREATE (surgeryCenter)-[:CONTRACTS_WITH]->(gpo)
CREATE (surgeryCenter)-[:REFERS_TO]->(imagingCenter)

// Example Queries

// Find all facilities within an IDN
MATCH (idn:IDN)-[r]->(facility)
RETURN idn.name, type(r), facility.name;

// Find all doctors with hospital privileges
MATCH (d:Doctor)-[:MEMBER_OF]->(:PhysicianGroup)-[:PRIVILEGES_AT]->(h:Hospital)
RETURN d.name, h.name;

// Find GPO relationships
MATCH (org)-[:PURCHASES_FROM|CONTRACTS_WITH]->(gpo:GPO)
RETURN org.name, gpo.name;

// Find referral patterns
MATCH (source)-[:REFERS_TO]->(target)
RETURN source.name, target.name;

// Find clinics and their operating organizations
MATCH (org)-[:OPERATES]->(c:Clinic)
RETURN org.name, c.name;

// Create indexes for better performance
CREATE INDEX ON :IDN(name);
CREATE INDEX ON :Hospital(name);
CREATE INDEX ON :Doctor(licenseNumber);
CREATE INDEX ON :GPO(name);

// Example of adding new relationships
MATCH (h:Hospital {name: 'Central Medical Center'})
MATCH (i:ImagingCenter {name: 'Precision Imaging'})
CREATE (h)-[:OWNS]->(i);

// Query to find complete care network
MATCH (idn:IDN)-[*1..2]->(facility)
RETURN idn.name, facility.name, labels(facility);

// Find all services available within a network
MATCH (idn:IDN)-[*1..2]->(facility)
RETURN DISTINCT labels(facility);

// Find overlapping GPO memberships
MATCH (org1)-[:PURCHASES_FROM]->(gpo:GPO)<-[:PURCHASES_FROM]-(org2)
WHERE id(org1) < id(org2)
RETURN org1.name, org2.name, gpo.name;
