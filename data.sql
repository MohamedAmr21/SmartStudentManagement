USE SmartStudentManagement;

SELECT Id, Email
FROM AspNetUsers;

SELECT Id, Name, NormalizedName
FROM AspNetRoles;

INSERT INTO AspNetUserRoles (UserId, RoleId)
VALUES ('09bd228e-bb77-4947-86e0-fd555a4af0b5', 'f57801db-51a7-40df-8ec9-b60904dc394f');

SELECT u.Email, r.Name AS Role
FROM AspNetUsers u
JOIN AspNetUserRoles ur ON u.Id = ur.UserId
JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Email = 'mohamed4078458@gmail.com';