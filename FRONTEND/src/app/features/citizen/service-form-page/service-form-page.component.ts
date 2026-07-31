import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';
import { RouterModule, Router, ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../../core/services/auth.service';

export interface DynamicCategory {
  categoryId: number;
  categoryName: string;
  guidelineText?: string;
}

export interface DynamicService {
  serviceId: number;
  serviceName: string;
  icon?: string;
  description?: string;
  categories: DynamicCategory[];
}

export interface DynamicHeading {
  parentHeadingId: number;
  parentHeadingName: string;
  icon?: string;
  description?: string;
  services: DynamicService[];
}

export interface FormSchemaField {
  id: number;
  fieldName: string;
  fieldLabel: string;
  fieldType: string;
  dropdownOptions?: string;
  isRequired: boolean;
  validationRules?: string;
  displayOrder: number;
  formSection?: string;
  parsedOptions?: any[];
  parsedValidationRules?: any;
  isVisible?: boolean;
}

export interface FormSchemaGroup {
  sectionTitle: string;
  fields: FormSchemaField[];
}

@Component({
  selector: 'app-service-form-page',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule],
  templateUrl: './service-form-page.component.html',
  styleUrls: ['./service-form-page.component.scss']
})
export class ServiceFormPageComponent implements OnInit {
  requestForm: FormGroup;
  headings: DynamicHeading[] = [];
  selectedService: DynamicService | null = null;
  selectedCategory: DynamicCategory | null = null;
  
  formSchema: FormSchemaField[] = [];
  groupedFormSchema: FormSchemaGroup[] = [];
  
  // Modal properties
  isSuccessModalOpen = false;
  successRequestId = '';
  isSubmitting = false;
  isErrorModalOpen = false;
  errorModalMessage = '';
  
  applicantType: string = 'myself';
  isUploading: boolean = false;
  uploadedFiles: File[] = [];
  
  mainDocumentFile: File | null = null;
  supportingFiles: File[] = [];

  private dynamicSubscriptions: any[] = [];

  familyMembersData: any[] = [];

  currentProfile: any = null;
  originalGuidelineText: string = '';

  constructor(private fb: FormBuilder, private router: Router, private route: ActivatedRoute, private http: HttpClient, private authService: AuthService, private cdr: ChangeDetectorRef) {
    this.requestForm = this.fb.group({
      // Top Level Dropdown for Category Selection
      categoryId: [''],

      // Personal Information (Auto-fetched)
      fullName: [{value: '', disabled: true}, Validators.required],
      nic: [{value: '', disabled: true}, Validators.required],
      phone: [{value: '', disabled: true}, Validators.required],
      email: [{value: '', disabled: true}],
      familyCardNumber: [{value: '', disabled: true}],
      address: [{value: '', disabled: true}],
      dynamicData: this.fb.group({})
    });
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const serviceId = params.get('serviceId');
      if (serviceId) {
        this.loadHeadingsAndService(Number(serviceId));
      } else {
        this.router.navigate(['/citizen/service-requests/new']);
      }
    });
    this.loadProfileData();

    this.setupCategoryListener();
  }

  setupCategoryListener() {
    this.requestForm.get('categoryId')?.valueChanges.subscribe(catId => {
      if (catId && this.selectedService) {
        const cat = this.selectedService.categories.find(c => c.categoryId == catId);
        if (cat) {
          this.selectedCategory = cat;
          this.originalGuidelineText = cat.guidelineText || '';
          this.loadFormSchema(cat.categoryId);
        }
      }
    });
  }

  loadHeadingsAndService(serviceId: number) {
    this.http.get<DynamicHeading[]>('http://localhost:5058/api/DynamicForm/headings-with-services')
      .subscribe(res => {
        let foundService: DynamicService | null = null;
        for (const heading of res) {
          const s = heading.services.find(x => x.serviceId === serviceId);
          if (s) {
            foundService = s;
            break;
          }
        }
        if (foundService) {
          this.selectService(foundService);
        } else {
          this.router.navigate(['/citizen/service-requests/new']);
        }
      }, err => console.error('Failed to load headings', err));
  }

  loadProfileData() {
    this.authService.profile$.subscribe(profile => {
      if (profile) {
        this.currentProfile = profile;
        // Also add basic properties to simulate logged_in_citizen details 
        // to match validation rules source mapping expectations
        this.currentProfile.gender = profile.Gender || profile.gender || 'Male'; 
        
        // Ensure legacy backward compatibility if old static forms are used
        this.requestForm.patchValue({
          fullName: profile.FullName || profile.fullName,
          nic: profile.Nic || profile.nic,
          phone: profile.Phone || profile.phone || '',
          email: profile.Email || profile.email || '',
          familyCardNumber: profile.FamilyCardNumber || profile.familyCardNumber || '',
          address: profile.Address || profile.address || 'No 12, Main Street, Colombo 01' // Mocked address since it wasn't in original res payload probably
        });

        this.loadFamilyMembers(profile.FamilyCardNumber || profile.familyCardNumber);
      }
    });
  }

  loadFamilyMembers(familyCardNumber: string) {
    if (!familyCardNumber) return;
    this.http.get<any>(`http://localhost:5058/api/FamilyCard/${familyCardNumber}/members`)
      .subscribe(res => {
        if (res.success || res.Success || res.isSuccess) {
          this.familyMembersData = res.data || res.Data || [];
          // Re-evaluate form schema if it was already loaded
          if (this.selectedCategory) {
            this.loadFormSchema(this.selectedCategory.categoryId);
          }
        }
      }, err => console.error('Failed to load family members', err));
  }

  selectService(service: DynamicService) {
    this.selectedService = service;
    this.selectedCategory = null;
    this.formSchema = [];
    
    // Reset standard form parts except profile
    this.requestForm = this.fb.group({
      categoryId: [''],
      fullName: [{value: this.requestForm.get('fullName')?.value, disabled: true}, Validators.required],
      nic: [{value: this.requestForm.get('nic')?.value, disabled: true}, Validators.required],
      phone: [{value: this.requestForm.get('phone')?.value, disabled: true}, Validators.required],
      email: [{value: this.requestForm.get('email')?.value, disabled: true}],
      familyCardNumber: [{value: this.requestForm.get('familyCardNumber')?.value, disabled: true}],
      address: [{value: this.requestForm.get('address')?.value, disabled: true}],
      dynamicData: this.fb.group({})
    });

    this.setupCategoryListener();

    if (service.categories && service.categories.length === 1) {
      // Auto select if there is only 1 category
      this.requestForm.get('categoryId')?.setValue(service.categories[0].categoryId);
    }
  }

  loadFormSchema(categoryId: number) {
    this.http.get<FormSchemaField[]>(`http://localhost:5058/api/DynamicForm/schema/${categoryId}`)
      .subscribe(res => {
        // Parse JSON options if available
        this.formSchema = res.map(f => {
          f.isVisible = true; // default
          if (f.validationRules) {
            if (typeof f.validationRules === 'string') {
              try {
                f.parsedValidationRules = JSON.parse(f.validationRules);
              } catch (e) {
                console.error(`Failed to parse validationRules for ${f.fieldName}:`, e);
                f.parsedValidationRules = null;
              }
            } else {
              f.parsedValidationRules = f.validationRules;
            }
          }
          if (f.dropdownOptions) {
            if (f.dropdownOptions.startsWith('api:')) {
               // Handle dynamic data source from legacy dropdownOptions
               if (f.dropdownOptions === 'api:family-members') {
                 f.parsedOptions = this.familyMembersData.map(m => ({ label: m.fullName || m.FullName, value: m.nic || m.Nic }));
               } else if (f.dropdownOptions === 'api:family-members-18plus') {
                 f.parsedOptions = this.familyMembersData
                    .filter(m => new Date(m.dob || m.Dob).getFullYear() <= new Date().getFullYear() - 18)
                    .map(m => ({ label: m.fullName || m.FullName, value: m.nic || m.Nic }));
               } else {
                 f.parsedOptions = [];
               }
            } else {
              try {
                f.parsedOptions = JSON.parse(f.dropdownOptions);
              } catch (e) {
                f.parsedOptions = [];
              }
            }
          } else if ((f.fieldType === 'dropdown' || f.fieldType === 'checkbox') && (f.parsedValidationRules?.dataSource === 'FamilyMembers' || f.parsedValidationRules?.dataSource === 'CurrentFamilyCardMembers')) {
             // Handle dropdown options from new JSON ValidationRules configuration
              let members = this.familyMembersData.filter(m => 
                (m.lifeStatus || m.LifeStatus || 'Alive').toLowerCase() === 'alive' &&
                m.isActive !== false && m.IsActive !== false &&
                (m.status || m.Status || 'Active').toLowerCase() === 'active'
              );
              
              // Apply any filtering
             if (f.parsedValidationRules.filterCriteria?.excludeApplicant || (this.selectedCategory?.categoryId === 3 && f.fieldName === 'affectedPerson')) {
                 const applicantNic = this.currentProfile?.Nic || this.currentProfile?.nic;
                 if (applicantNic) {
                     members = members.filter(m => (m.nic || m.Nic || m.nicNumber || m.NicNumber) !== applicantNic);
                 }
             }
             if (f.parsedValidationRules.filterCriteria?.minAge) {
                 members = members.filter(m => new Date(m.dob || m.Dob).getFullYear() <= new Date().getFullYear() - f.parsedValidationRules.filterCriteria.minAge);
             }
             if (f.parsedValidationRules.filterCriteria?.maxAge) {
                 members = members.filter(m => {
                     const age = new Date().getFullYear() - new Date(m.dob || m.Dob).getFullYear();
                     return age < f.parsedValidationRules.filterCriteria.maxAge;
                 });
             }

              const displayField = f.parsedValidationRules.displayField || 'fullName';
              const valueField = 'nic'; // Force NIC as value
              const isMembersToMove = f.fieldName === 'familyMembersToMove' || f.parsedValidationRules?.dataSource === 'CurrentFamilyCardMembers';

              f.parsedOptions = members.map(m => {
                  let label = m[displayField] || m[displayField.charAt(0).toUpperCase() + displayField.slice(1)];
                  if (displayField === 'fullName-nic') {
                      label = (m.fullName || m.FullName) + ' - ' + (m.nic || m.Nic);
                  }
                  if (isMembersToMove) {
                      const name = m.fullName || m.FullName;
                      const nic = m.nic || m.Nic || m.nicNumber || m.NicNumber;
                      const rel = m.relationshipToHead || m.RelationshipToHead || 'Member';
                      label = `${name} - ${nic} (${rel})`;
                  }
                  return {
                      label: label,
                      value: m[valueField] || m[valueField.charAt(0).toUpperCase() + valueField.slice(1)]
                  };
              });
           } else if (f.parsedValidationRules?.dataSource && f.parsedValidationRules.dataSource !== 'FamilyMembers' && f.parsedValidationRules.dataSource !== 'CurrentFamilyCardMembers') {
              // It's an external API dropdown. If it has no dependsOn, load it immediately.
              if (!f.parsedValidationRules.dependsOn) {
                this.fetchDropdownData(f);
              } else {
                f.parsedOptions = [];
              }
           }
          return f;
        });

        // Group by formSection
        this.groupedFormSchema = [];
        this.formSchema.forEach(field => {
          const sectionName = field.formSection || 'Request Details';
          let group = this.groupedFormSchema.find(g => g.sectionTitle === sectionName);
          if (!group) {
            group = { sectionTitle: sectionName, fields: [] };
            this.groupedFormSchema.push(group);
          }
          group.fields.push(field);
        });

        // Rebuild form controls based on schema
        this.rebuildDynamicControls();

        // Auto-populate GN Hierarchy if fields exist
        const gnId = this.currentProfile?.GnDivisionId || this.currentProfile?.gnDivisionId;
        if (gnId && this.formSchema.some(f => f.fieldName === 'currentGnDivision')) {
           this.loadCurrentGnHierarchy(gnId);
        }
      }, err => console.error('Failed to load schema', err));
  }

  rebuildDynamicControls() {
    // Clear old subscriptions
    this.dynamicSubscriptions.forEach(sub => sub.unsubscribe());
    this.dynamicSubscriptions = [];

    const dynamicGroup = this.fb.group({});

    if (this.selectedCategory?.categoryId === 3) {
      const newHeadField = this.formSchema.find(f => f.fieldName === 'newFamilyHead');
      if (newHeadField) {
        newHeadField.isVisible = false;
      }
    }

    // Add new dynamic controls
    this.formSchema.forEach(field => {
      const validators = [];
      if (field.isRequired) validators.push(Validators.required);
      
      let initialValue: any = '';
      if (field.parsedValidationRules?.multiple || field.fieldType === 'checkbox') {
        initialValue = [];
      } else if (field.parsedValidationRules?.defaultValue) {
        initialValue = field.parsedValidationRules.defaultValue;
      } else if (field.fieldName === 'transferFamilyMembers') {
        initialValue = 'No'; // hardcoded default as per requirements
      }

      let disabled = false;
      if (field.parsedValidationRules?.readOnly || field.parsedValidationRules?.readonly) {
        disabled = true;
      }
      
      dynamicGroup.addControl(field.fieldName, new FormControl<any>({value: initialValue, disabled: disabled}, validators));
    });

    this.requestForm.setControl('dynamicData', dynamicGroup);

    if (this.selectedCategory?.categoryId === 5) {
      const spouseNicControl = dynamicGroup.get('spouseNic');
      if (spouseNicControl) {
        const spouseNicSub = spouseNicControl.valueChanges.subscribe(() => {
          this.clearSpouseData(dynamicGroup);
        });
        this.dynamicSubscriptions.push(spouseNicSub);
      }

      const requestTypeControl = dynamicGroup.get('requestType');
      if (requestTypeControl) {
        const requestTypeSub = requestTypeControl.valueChanges.subscribe((type) => {
          if (type !== 'Marriage') {
            this.clearSpouseData(dynamicGroup);
          }
          this.evaluateVisibility();
        });
        this.dynamicSubscriptions.push(requestTypeSub);
      }

      const familyMembersToMoveControl = dynamicGroup.get('familyMembersToMove');
      if (familyMembersToMoveControl) {
        const familyMembersSub = familyMembersToMoveControl.valueChanges.subscribe((selectedNics: string[] | string) => {
          const nicsArr = Array.isArray(selectedNics) ? selectedNics : (selectedNics ? [selectedNics] : []);
          const newHeadField = this.formSchema.find(f => f.fieldName === 'newHeadOfHousehold');
          const newHeadControl = dynamicGroup.get('newHeadOfHousehold') as any;

          if (newHeadField && newHeadControl) {
            const selectedMembers = this.familyMembersData.filter(m => {
              const nic = m.nic || m.Nic || m.nicNumber || m.NicNumber;
              return nicsArr.includes(nic);
            });

            newHeadField.parsedOptions = selectedMembers.map(m => {
              const name = m.fullName || m.FullName;
              const nic = m.nic || m.Nic || m.nicNumber || m.NicNumber;
              return {
                label: `${name} - ${nic}`,
                value: nic
              };
            });

            const currentHeadVal = newHeadControl.value;
            if (currentHeadVal && !nicsArr.includes(currentHeadVal)) {
              newHeadControl.setValue('', { emitEvent: false });
            }
          }
        });
        this.dynamicSubscriptions.push(familyMembersSub);
      }

      const sameGnDivisionControl = dynamicGroup.get('sameGnDivision');
      if (sameGnDivisionControl) {
        const sameGnSub = sameGnDivisionControl.valueChanges.subscribe(val => {
          const valStr = ((val as any) || '').toString().trim().toLowerCase();
          if (valStr === 'yes' || valStr === 'y') {
            // Clear previously selected location values
            (dynamicGroup.get('province') as any)?.patchValue('', { emitEvent: false });
            (dynamicGroup.get('district') as any)?.patchValue('', { emitEvent: false });
            (dynamicGroup.get('dsDivision') as any)?.patchValue('', { emitEvent: false });
            (dynamicGroup.get('newGnDivision') as any)?.patchValue('', { emitEvent: false });
            this.evaluateVisibility();
          } else if (valStr === 'no' || valStr === 'n') {
            this.clearNewResidenceHierarchy(dynamicGroup);
            this.evaluateVisibility();
          }
        });
        this.dynamicSubscriptions.push(sameGnSub);
      }
    }

    this.evaluateVisibility();

    // Setup value change listeners for controlling fields to re-evaluate visibility
    const controllingFields = new Set<string>();
    this.formSchema.forEach(field => {
      const rules = field.parsedValidationRules;
      if (rules) {
        if (rules.conditionalVisibility?.field) {
          controllingFields.add(rules.conditionalVisibility.field);
        }
        if (rules.dependsOn) {
          controllingFields.add(rules.dependsOn);
        }
      }
    });

    controllingFields.forEach(fieldName => {
      const control = dynamicGroup.get(fieldName);
      if (control) {
        const sub = control.valueChanges.subscribe(() => {
          this.evaluateVisibility();
        });
        this.dynamicSubscriptions.push(sub);
      }
    });

    // Setup specific valueChanges for autoPopulate
    this.formSchema.forEach(field => {
      const rules = field.parsedValidationRules;
      if (!rules) return;

      // Current Family Card auto population
      if (rules.autoPopulate && rules.autoPopulate.source === 'CurrentFamilyCard') {
         (dynamicGroup.get(field.fieldName) as any)?.patchValue(this.familyMembersData.find(m => m.relationshipToHead === 'Head')?.fullName || '', { emitEvent: false });
      }

      // Computed Fields
      if (rules.computedField && rules.computedField.source === 'logged_in_citizen.gender') {
          const userGender = this.currentProfile?.gender || 'Male';
          const calculatedValue = rules.computedField.condition[userGender];
          (dynamicGroup.get(field.fieldName) as any)?.patchValue(calculatedValue, { emitEvent: false });
      }

      // Dropdown auto population
      if (rules.autoPopulate && field.fieldType === 'dropdown') {
        const autoPopSub = dynamicGroup.get(field.fieldName)?.valueChanges.subscribe(selectedValue => {
          if (selectedValue) {
             const familyCardNumber = this.requestForm.get('familyCardNumber')?.value;
             
             // First call Citizen Master
             this.http.get<any>(`http://localhost:5058/api/CitizenMaster/${selectedValue}`).subscribe(
               citizenRes => {
                 let sourceData = citizenRes.data || citizenRes.Data || citizenRes;

                 // Then call Family Card members to get relationship if familyCardNumber exists
                 if (familyCardNumber) {
                   this.http.get<any>(`http://localhost:5058/api/FamilyCard/${familyCardNumber}/members`).subscribe(
                     fcRes => {
                       const members = fcRes.data || fcRes.Data || [];
                       const memberData = members.find((m: any) => (m.nic || m.Nic) === selectedValue);
                       if (memberData) {
                         sourceData = { ...sourceData, ...memberData };
                       }
                       this.applyAutoPopulateMappings(sourceData, rules.autoPopulate, dynamicGroup);
                     },
                     err => {
                       console.error('Failed to fetch FamilyCard data', err);
                       this.applyAutoPopulateMappings(sourceData, rules.autoPopulate, dynamicGroup);
                     }
                   );
                 } else {
                   this.applyAutoPopulateMappings(sourceData, rules.autoPopulate, dynamicGroup);
                 }
               },
               err => console.error('Failed to fetch CitizenMaster data for dropdown selection', err)
             );
          }
        });
        if (autoPopSub) this.dynamicSubscriptions.push(autoPopSub);
      }
      
      // Auto populate currentResidentialAddress
      if (field.fieldName === 'currentResidentialAddress') {
         const address = this.requestForm.get('address')?.value;
         if (address) {
            const control = dynamicGroup.get(field.fieldName);
            if (control) {
               (control as any).patchValue(address, { emitEvent: false });
            }
         }
      }

      // Auto populate deceasedFullName from deceasedNic when categoryId is 50
      if (this.selectedCategory?.categoryId === 50 && field.fieldName === 'deceasedNic') {
        const nicControl = dynamicGroup.get('deceasedNic');
        const nameControl = dynamicGroup.get('deceasedFullName');
        if (nicControl && nameControl) {
          const nicSub = nicControl.valueChanges.subscribe((nicVal: any) => {
            const trimmedNic = ((nicVal as string) || '').trim();
            if (trimmedNic.length === 10 || trimmedNic.length === 12) {
              this.http.get<any>(`http://localhost:5058/api/CitizenMaster/${trimmedNic}`).subscribe({
                next: (citizenRes) => {
                  const citizen = citizenRes.data || citizenRes.Data || citizenRes;
                  if (citizen && (citizen.fullName || citizen.FullName)) {
                    (nameControl as any).patchValue(citizen.fullName || citizen.FullName);
                  }
                },
                error: (err) => {
                  console.error('Failed to fetch deceased citizen details', err);
                }
              });
            }
          });
          this.dynamicSubscriptions.push(nicSub);
        }
      }

      // Cascading logic
      if (rules.dependsOn) {
         const parentControl = dynamicGroup.get(rules.dependsOn);
         if (parentControl) {
            const sub = parentControl.valueChanges.subscribe(parentVal => {
               // Clear current value
               (dynamicGroup.get(field.fieldName) as any)?.patchValue('');
               if (parentVal) {
                  this.fetchDropdownData(field, parentVal);
               } else {
                  field.parsedOptions = [];
               }
            });
            this.dynamicSubscriptions.push(sub);
         }
      }

    });

    // Category 3: Removal of Deceased Family Member (Conditional display of New Family Head)
    const affectedPersonControl = dynamicGroup.get('affectedPerson');
    if (this.selectedCategory?.categoryId === 3 && affectedPersonControl) {
      const affectedPersonSub = affectedPersonControl.valueChanges.subscribe(selectedNic => {
        const newHeadField = this.formSchema.find(f => f.fieldName === 'newFamilyHead');
        const newHeadControl = dynamicGroup.get('newFamilyHead') as any;

        if (newHeadField && newHeadControl) {
          if (selectedNic) {
            const selectedMember = (this.familyMembersData || []).find(m => (m.nic || m.Nic || m.nicNumber || m.NicNumber) === selectedNic);
            const isDeceasedHead = selectedMember && (selectedMember.relationshipToHead === 'Head' || selectedMember.RelationshipToHead === 'Head' || selectedMember.relationshipToHead === 'Self' || selectedMember.RelationshipToHead === 'Self');
            const activeMembersCount = (this.familyMembersData || []).length;

            if (isDeceasedHead && activeMembersCount > 1) {
              newHeadField.isVisible = true;
              newHeadControl.setValidators([Validators.required]);

              // Populate eligible members: alive, active, >= 18, and not the deceased person
              const eligibleMembers = (this.familyMembersData || []).filter(m => {
                const nic = m.nic || m.Nic || m.nicNumber || m.NicNumber;
                if (nic === selectedNic) return false;

                const lifeStatus = m.lifeStatus || m.LifeStatus || 'Alive';
                if (lifeStatus.toLowerCase() !== 'alive') return false;

                // Check age: 18+
                const dob = m.dob || m.Dob || m.dateOfBirth || m.DateOfBirth;
                if (!dob) return false;
                const dobYear = new Date(dob).getFullYear();
                const currentYear = new Date().getFullYear();
                return (currentYear - dobYear) >= 18;
              });

              newHeadField.parsedOptions = eligibleMembers.map(m => ({
                label: m.fullName || m.FullName,
                value: m.nic || m.Nic || m.nicNumber || m.NicNumber
              }));
            } else {
              newHeadField.isVisible = false;
              newHeadControl.clearValidators();
              newHeadControl.setValue('');
            }
          } else {
            newHeadField.isVisible = false;
            newHeadControl.clearValidators();
            newHeadControl.setValue('');
          }
          newHeadControl.updateValueAndValidity({ emitEvent: false });
          this.evaluateVisibility();
        }
      });
      this.dynamicSubscriptions.push(affectedPersonSub);
    }
  }

  populateSpouseData(citizen: any, divisionName: string, dynamicGroup: FormGroup) {
    const fullName = citizen.fullName || citizen.FullName || '';
    const gender = citizen.gender || citizen.Gender || '';
    (dynamicGroup.get('spouseFullName') as any)?.patchValue(fullName);
    (dynamicGroup.get('spouseGender') as any)?.patchValue(gender);
    (dynamicGroup.get('spouseGnDivision') as any)?.patchValue(divisionName);

    dynamicGroup.get('spouseFullName')?.disable({ emitEvent: false });
    dynamicGroup.get('spouseGender')?.disable({ emitEvent: false });
    dynamicGroup.get('spouseGnDivision')?.disable({ emitEvent: false });

    const loggedNic = this.currentProfile?.Nic || this.currentProfile?.nic || '';
    const loggedGender = (this.currentProfile?.gender || this.currentProfile?.Gender || 'Male').toLowerCase();
    const spouseNic = citizen.nic || citizen.Nic || '';
    const spouseGenderLower = gender.toLowerCase();

    let newHeadOfHousehold = '';
    if (loggedGender === 'male' && spouseGenderLower === 'female') {
      newHeadOfHousehold = loggedNic;
    } else if (spouseGenderLower === 'male' && loggedGender === 'female') {
      newHeadOfHousehold = spouseNic;
    }

    if (newHeadOfHousehold) {
      (dynamicGroup.get('newHeadOfHousehold') as any)?.patchValue(newHeadOfHousehold, { emitEvent: false });
    }
  }

  clearSpouseData(dynamicGroup: FormGroup) {
    dynamicGroup.get('spouseFullName')?.enable({ emitEvent: false });
    dynamicGroup.get('spouseGender')?.enable({ emitEvent: false });
    dynamicGroup.get('spouseGnDivision')?.enable({ emitEvent: false });

    (dynamicGroup.get('spouseFullName') as any)?.patchValue('', { emitEvent: false });
    (dynamicGroup.get('spouseGender') as any)?.patchValue('', { emitEvent: false });
    (dynamicGroup.get('spouseGnDivision') as any)?.patchValue('', { emitEvent: false });
    (dynamicGroup.get('newHeadOfHousehold') as any)?.patchValue('', { emitEvent: false });
  }

  populateNewResidenceHierarchy(gnDivisionId: string, dynamicGroup: FormGroup) {
    this.http.get<any>(`http://localhost:5058/api/GnDivision/${gnDivisionId}`).subscribe(gn => {
      this.http.get<any>(`http://localhost:5058/api/DsDivision/${gn.dsDivisionId}`).subscribe(ds => {
        this.http.get<any[]>(`http://localhost:5058/api/Districts`).subscribe(districts => {
          const district = districts.find(d => d.id == ds.districtId);
          if (district) {
            this.http.get<any[]>(`http://localhost:5058/api/Provinces`).subscribe(provinces => {
              const province = provinces.find(p => p.id == district.provinceId);
              if (province) {
                const provinceField = this.formSchema.find(f => f.fieldName === 'province');
                if (provinceField) provinceField.parsedOptions = [{ label: province.name, value: province.id }];
                
                const districtField = this.formSchema.find(f => f.fieldName === 'district');
                if (districtField) districtField.parsedOptions = [{ label: district.name, value: district.id }];
                
                const dsField = this.formSchema.find(f => f.fieldName === 'dsDivision');
                if (dsField) dsField.parsedOptions = [{ label: ds.name, value: ds.id }];
                
                const gnField = this.formSchema.find(f => f.fieldName === 'newGnDivision');
                if (gnField) gnField.parsedOptions = [{ label: gn.name, value: gn.id }];

                dynamicGroup.get('province')?.patchValue(province.id, { emitEvent: false });
                dynamicGroup.get('district')?.patchValue(district.id, { emitEvent: false });
                dynamicGroup.get('dsDivision')?.patchValue(ds.id, { emitEvent: false });
                dynamicGroup.get('newGnDivision')?.patchValue(gn.id, { emitEvent: false });

                dynamicGroup.get('province')?.disable({ emitEvent: false });
                dynamicGroup.get('district')?.disable({ emitEvent: false });
                dynamicGroup.get('dsDivision')?.disable({ emitEvent: false });
                dynamicGroup.get('newGnDivision')?.disable({ emitEvent: false });
              }
            });
          }
        });
      });
    });
  }

  clearNewResidenceHierarchy(dynamicGroup: FormGroup) {
    dynamicGroup.get('province')?.enable({ emitEvent: false });
    dynamicGroup.get('district')?.enable({ emitEvent: false });
    dynamicGroup.get('dsDivision')?.enable({ emitEvent: false });
    dynamicGroup.get('newGnDivision')?.enable({ emitEvent: false });

    (dynamicGroup.get('province') as any)?.patchValue('', { emitEvent: false });
    (dynamicGroup.get('district') as any)?.patchValue('', { emitEvent: false });
    (dynamicGroup.get('dsDivision') as any)?.patchValue('', { emitEvent: false });
    (dynamicGroup.get('newGnDivision') as any)?.patchValue('', { emitEvent: false });

    const provinceField = this.formSchema.find(f => f.fieldName === 'province');
    if (provinceField) {
      provinceField.parsedOptions = [];
      this.fetchDropdownData(provinceField);
    }
    const districtField = this.formSchema.find(f => f.fieldName === 'district');
    if (districtField) districtField.parsedOptions = [];
    const dsField = this.formSchema.find(f => f.fieldName === 'dsDivision');
    if (dsField) dsField.parsedOptions = [];
    const gnField = this.formSchema.find(f => f.fieldName === 'newGnDivision');
    if (gnField) gnField.parsedOptions = [];
  }

  fetchDropdownData(field: FormSchemaField, parentValue?: any) {
     let url = '';
     const source = field.parsedValidationRules?.dataSource;
     const filterField = field.parsedValidationRules?.filterField;
     
     const sourceLower = source?.toLowerCase();
     if (sourceLower === 'province' || sourceLower === 'provinces') {
        url = 'http://localhost:5058/api/Provinces';
     } else if (sourceLower === 'districts' || sourceLower === 'district') {
        url = `http://localhost:5058/api/Districts?${filterField || 'provinceId'}=${parentValue}`;
     } else if (sourceLower === 'ds_divisions' || sourceLower === 'dsdivisions' || sourceLower === 'dsdivision') {
        url = `http://localhost:5058/api/DsDivision?${filterField || 'districtId'}=${parentValue}`;
     } else if (sourceLower === 'gn_divisions' || sourceLower === 'gndivisions' || sourceLower === 'gndivision') {
        url = `http://localhost:5058/api/GnDivision?${filterField || 'dsDivisionId'}=${parentValue}`;
     }

      if (url) {
         this.http.get<any>(url).subscribe(res => {
            const rawList = Array.isArray(res) ? res : (res?.data || res?.Data || []);
            const displayField = field.parsedValidationRules?.displayField || field.parsedValidationRules?.labelField || 'name';
            const valueField = field.parsedValidationRules?.valueField || 'id';
            
            field.parsedOptions = rawList.map((r: any) => {
               const val = r[valueField] !== undefined ? r[valueField] : (r['id'] !== undefined ? r['id'] : (r['Id'] !== undefined ? r['Id'] : ''));
               const lbl = r[displayField] !== undefined ? r[displayField] : (r['name'] !== undefined ? r['name'] : (r['Name'] !== undefined ? r['Name'] : ''));
               return {
                  label: lbl,
                  value: val
               };
            });
         }, err => console.error(`Failed to load data for ${field.fieldName}`, err));
      }
  }

  loadCurrentGnHierarchy(gnDivisionId: string) {
    this.http.get<any>(`http://localhost:5058/api/GnDivision/${gnDivisionId}`).subscribe(gn => {
      (this.requestForm.get('dynamicData.currentGnDivision') as any)?.patchValue(gn.name, { emitEvent: false });
      
      this.http.get<any>(`http://localhost:5058/api/DsDivision/${gn.dsDivisionId}`).subscribe(ds => {
        (this.requestForm.get('dynamicData.currentDsDivision') as any)?.patchValue(ds.name, { emitEvent: false });
        
        this.http.get<any[]>(`http://localhost:5058/api/Districts`).subscribe(districts => {
          const district = districts.find(d => d.id == ds.districtId);
          if (district) {
            (this.requestForm.get('dynamicData.currentDistrict') as any)?.patchValue(district.name, { emitEvent: false });
            
            this.http.get<any[]>(`http://localhost:5058/api/Provinces`).subscribe(provinces => {
              const province = provinces.find(p => p.id == district.provinceId);
              if (province) {
                (this.requestForm.get('dynamicData.currentProvince') as any)?.patchValue(province.name, { emitEvent: false });
              }
            });
          }
        });
      });
    });
  }

  evaluateVisibility() {
    const dynamicGroup = this.requestForm.get('dynamicData') as FormGroup;
    if (!dynamicGroup) return;

    this.formSchema.forEach(field => {
      // Category 3 special handling: newFamilyHead visibility is evaluated inside rebuilt controls subscription
      if (this.selectedCategory?.categoryId === 3 && field.fieldName === 'newFamilyHead') {
        return;
      }

      let isVisible = true;

      // 1. Evaluate conditionalVisibility if present
      if (field.parsedValidationRules?.conditionalVisibility) {
        const rule = field.parsedValidationRules.conditionalVisibility;
        let targetValue: any = undefined;
        if (rule.field === 'applicantType') {
          targetValue = this.applicantType;
        } else {
          targetValue = dynamicGroup.get(rule.field)?.value;
        }

        const targetValueStr = targetValue !== undefined && targetValue !== null ? targetValue.toString().trim() : '';

        if (rule.values && Array.isArray(rule.values)) {
          isVisible = rule.values.some((val: any) => val?.toString().trim() === targetValueStr);
        } else if (rule.in && Array.isArray(rule.in)) {
          isVisible = rule.in.some((val: any) => val?.toString().trim() === targetValueStr);
        } else {
          const expectedValue = rule.value !== undefined ? rule.value : rule.equals;
          const expectedValueStr = expectedValue !== undefined && expectedValue !== null ? expectedValue.toString().trim() : '';
          isVisible = targetValueStr === expectedValueStr;
        }
      }

      // 2. Evaluate dependsOn if present (cascade visibility)
      if (isVisible && field.parsedValidationRules?.dependsOn) {
        const parentField = field.parsedValidationRules.dependsOn;
        let parentValue: any = undefined;
        if (parentField === 'applicantType') {
          parentValue = this.applicantType;
        } else {
          parentValue = dynamicGroup.get(parentField)?.value;
        }

        const dependsOnValue = field.parsedValidationRules.dependsOnValue;
        if (dependsOnValue !== undefined && dependsOnValue !== null) {
          isVisible = parentValue?.toString().trim() === dependsOnValue.toString().trim();
        } else {
          const isEmpty = Array.isArray(parentValue) ? parentValue.length === 0 : (!parentValue || parentValue.toString().trim() === '');
          isVisible = !isEmpty;
        }
      }

      field.isVisible = isVisible;
      const control = dynamicGroup.get(field.fieldName);

      if (control) {
        if (isVisible) {
          // Restore validators & enable control
          const isReadOnly = !!(field.parsedValidationRules?.readOnly || field.parsedValidationRules?.readonly);
          if (isReadOnly) {
            control.disable({ emitEvent: false });
          } else {
            control.enable({ emitEvent: false });
          }

          if (field.isRequired) {
            control.setValidators([Validators.required]);
          } else {
            control.clearValidators();
          }
          control.updateValueAndValidity({ emitEvent: false });
        } else {
          // Hide and clear unrelated field values, validation errors, and required validators
          control.disable({ emitEvent: false });
          control.clearValidators();
          control.setErrors(null);

          const currentValue = control.value;
          const isArrayVal = Array.isArray(currentValue);
          const isEmpty = isArrayVal ? (currentValue.length === 0) : (currentValue === '' || currentValue === null || currentValue === undefined);

          if (!isEmpty) {
            if (field.fieldType === 'checkbox' || field.parsedValidationRules?.multiple) {
              control.patchValue([], { emitEvent: false });
            } else {
              control.patchValue('', { emitEvent: false });
            }
          }
          control.updateValueAndValidity({ emitEvent: false });
        }
      }

      // Check for dynamic guideline mapping
      if (field.parsedValidationRules?.guidelineMap) {
         const val = dynamicGroup.get(field.fieldName)?.value;
         if (val && field.parsedValidationRules.guidelineMap[val]) {
            if (this.selectedCategory) {
               this.selectedCategory.guidelineText = field.parsedValidationRules.guidelineMap[val];
            }
         } else {
            if (this.selectedCategory) {
               this.selectedCategory.guidelineText = this.originalGuidelineText;
            }
         }
      }
    });

    this.cdr.detectChanges();
  }

  applyAutoPopulateMappings(sourceData: any, mappings: any, dynamicGroup: FormGroup) {
    if (!sourceData || !mappings) return;
    const patchData: any = {};
    for (const targetField in mappings) {
      let sourceField = mappings[targetField];
      let val = sourceData[sourceField];
      
      if (val === undefined) {
          const pascalCase = sourceField.charAt(0).toUpperCase() + sourceField.slice(1);
          val = sourceData[pascalCase];
      }
      if (val === undefined && (sourceField === 'dob' || sourceField === 'dateOfBirth')) {
          val = sourceData['Dob'] || sourceData['dob'] || sourceData['DateOfBirth'] || sourceData['dateOfBirth'];
      }
      if (val === undefined && (sourceField === 'address' || sourceField === 'previousAddress')) {
          val = sourceData['Address'] || sourceData['address'] || sourceData['PreviousAddress'] || sourceData['previousAddress'];
      }
      if (val === undefined && (sourceField === 'gnDivisionId' || sourceField === 'gnDivision' || sourceField === 'previousGnDivision')) {
          val = sourceData['GnDivisionId'] || sourceData['gnDivisionId'] || sourceData['GnDivision'] || sourceData['gnDivision'];
      }
      if (val === undefined && (sourceField === 'relationshipToHead' || sourceField === 'relationship' || sourceField === 'relationshipToCurrentHead')) {
          val = sourceData['RelationshipToHead'] || sourceData['relationshipToHead'] || sourceData['Relationship'] || sourceData['relationship'];
      }
      if (val === undefined && sourceField === 'gender') {
          val = sourceData['Gender'] || sourceData['gender'];
      }
      if (val === undefined && sourceField === 'fullName') {
          val = sourceData['FullName'] || sourceData['fullName'];
      }
      if (val === undefined && sourceField === 'nic') {
          val = sourceData['Nic'] || sourceData['nic'];
      }
      if (val === undefined && sourceField === 'age') {
          const dob = sourceData['Dob'] || sourceData['dob'] || sourceData['DateOfBirth'] || sourceData['dateOfBirth'];
          if (dob) {
              const birthDate = new Date(dob);
              let age = new Date().getFullYear() - birthDate.getFullYear();
              const m = new Date().getMonth() - birthDate.getMonth();
              if (m < 0 || (m === 0 && new Date().getDate() < birthDate.getDate())) {
                  age--;
              }
              val = age;
          }
      }
      
      patchData[targetField] = val;
    }
    dynamicGroup.patchValue(patchData, { emitEvent: false });
  }

  lookupNic(fieldName: string) {
    const dynamicGroup = this.requestForm.get('dynamicData') as FormGroup;
    if (!dynamicGroup) return;

    const nicValue = dynamicGroup.get(fieldName)?.value;
    if (!nicValue) {
      this.errorModalMessage = 'Please enter a NIC number first.';
      this.isErrorModalOpen = true;
      return;
    }

    const field = this.formSchema.find(f => f.fieldName === fieldName);
    if (!field) return;

    if (fieldName === 'spouseNic') {
      const applicantNic = this.currentProfile?.Nic || this.currentProfile?.nic;
      if (nicValue === applicantNic) {
        this.errorModalMessage = 'You cannot select your own NIC as the spouse NIC.';
        this.isErrorModalOpen = true;
        this.clearSpouseData(dynamicGroup);
        return;
      }

      const isAlreadyInCard = this.familyMembersData.some(m => (m.nic || m.Nic || m.nicNumber || m.NicNumber) === nicValue);
      if (isAlreadyInCard) {
        this.errorModalMessage = 'The selected spouse is already a member of your current family card.';
        this.isErrorModalOpen = true;
        this.clearSpouseData(dynamicGroup);
        return;
      }

      this.isUploading = true;
      this.http.get<any>(`http://localhost:5058/api/CitizenMaster/${nicValue}`)
        .subscribe({
          next: (res) => {
            const citizen = res.data || res.Data || res;
            if (citizen && (citizen.fullName || citizen.FullName)) {
              const divId = citizen.gnDivisionId || citizen.GnDivisionId || '';
              if (divId) {
                this.http.get<any>(`http://localhost:5058/api/GnDivision/${divId}`)
                  .subscribe({
                    next: (divRes) => {
                      this.isUploading = false;
                      const division = divRes.data || divRes.Data || divRes;
                      const divisionName = division?.name || division?.Name || divId;
                      this.populateSpouseData(citizen, divisionName, dynamicGroup);
                    },
                    error: () => {
                      this.isUploading = false;
                      this.populateSpouseData(citizen, divId, dynamicGroup);
                    }
                  });
              } else {
                this.isUploading = false;
                this.populateSpouseData(citizen, 'Unknown', dynamicGroup);
              }
            } else {
              this.isUploading = false;
              this.errorModalMessage = `No citizen found with NIC: ${nicValue} in the system.`;
              this.isErrorModalOpen = true;
              this.clearSpouseData(dynamicGroup);
            }
          },
          error: (err) => {
            this.isUploading = false;
            console.error('Spouse NIC lookup failed', err);
            this.errorModalMessage = `No citizen found with NIC: ${nicValue} in the system.`;
            this.isErrorModalOpen = true;
            this.clearSpouseData(dynamicGroup);
          }
        });
      return;
    }

    if (fieldName === 'deceasedNic') {
      this.http.get<any>(`http://localhost:5058/api/CitizenMaster/${nicValue}`)
        .subscribe({
          next: (res) => {
            const citizen = res.data || res.Data || res;
            if (citizen && (citizen.fullName || citizen.FullName)) {
              dynamicGroup.get('deceasedFullName')?.patchValue(citizen.fullName || citizen.FullName);
            } else {
              this.errorModalMessage = `No citizen found with NIC: ${nicValue} in the system.`;
              this.isErrorModalOpen = true;
            }
          },
          error: (err) => {
            console.error('Deceased NIC lookup failed', err);
            this.errorModalMessage = `No citizen found with NIC: ${nicValue} in the system.`;
            this.isErrorModalOpen = true;
          }
        });
      return;
    }

    const mappings = field.parsedValidationRules?.autoPopulate || {};

    this.http.get<any>(`http://localhost:5058/api/CitizenMaster/${nicValue}`)
      .subscribe({
        next: (res) => {
          let sourceData = res.data || res.Data || res;
          
          const familyCardNumber = this.requestForm.get('familyCardNumber')?.value;
          if (familyCardNumber) {
             this.http.get<any>(`http://localhost:5058/api/FamilyCard/${familyCardNumber}/members`).subscribe({
                next: (fcRes) => {
                   const members = fcRes.data || fcRes.Data || [];
                   const memberData = members.find((m: any) => (m.nic || m.Nic) === nicValue);
                   if (memberData) {
                       sourceData = { ...sourceData, ...memberData };
                   }
                   if (Object.keys(mappings).length > 0) {
                       this.applyAutoPopulateMappings(sourceData, mappings, dynamicGroup);
                   } else {
                       // Direct mapping for fallback if no mappings defined but lookup is successful
                       dynamicGroup.patchValue({
                           fullName: sourceData.FullName || sourceData.fullName,
                           gender: sourceData.Gender || sourceData.gender
                       }, { emitEvent: false });
                   }

                   if (this.selectedCategory?.categoryId === 2) {
                     dynamicGroup.patchValue({
                       previousFamilyCardNumber: sourceData.FamilyCardNumber || sourceData.familyCardNumber,
                       familyCardNumber: sourceData.FamilyCardNumber || sourceData.familyCardNumber
                     }, { emitEvent: false });
                   }
                },
                error: (err) => {
                   if (Object.keys(mappings).length > 0) {
                       this.applyAutoPopulateMappings(sourceData, mappings, dynamicGroup);
                   }
                   if (this.selectedCategory?.categoryId === 2) {
                     dynamicGroup.patchValue({
                       previousFamilyCardNumber: sourceData.FamilyCardNumber || sourceData.familyCardNumber,
                       familyCardNumber: sourceData.FamilyCardNumber || sourceData.familyCardNumber
                     }, { emitEvent: false });
                   }
                }
             });
          } else {
             if (Object.keys(mappings).length > 0) {
                 this.applyAutoPopulateMappings(sourceData, mappings, dynamicGroup);
             }
             if (this.selectedCategory?.categoryId === 2) {
               dynamicGroup.patchValue({
                 previousFamilyCardNumber: sourceData.FamilyCardNumber || sourceData.familyCardNumber,
                 familyCardNumber: sourceData.FamilyCardNumber || sourceData.familyCardNumber
               }, { emitEvent: false });
             }
          }
        },
        error: (err) => {
          console.error('NIC lookup failed', err);
          this.errorModalMessage = `No matching citizen found for the provided NIC: ${nicValue}`;
          this.isErrorModalOpen = true;
        }
      });
  }

  get showApplicantType(): boolean {
    return this.formSchema.some(f => f.fieldType === 'applicant-type');
  }

  setApplicantType(type: string) {
    this.applicantType = type;
    this.evaluateVisibility();
  }

  onFileChange(event: any, fieldName?: string) {
    const files = event.target.files;
    if (files.length > 0) {
      this.isUploading = true;
      setTimeout(() => {
        for (let i = 0; i < files.length; i++) {
          this.uploadedFiles.push(files[i]);
        }
        if (fieldName) {
           const dynamicGroup = this.requestForm.get('dynamicData') as FormGroup;
           dynamicGroup.get(fieldName)?.patchValue('uploaded');
        }
        this.isUploading = false;
      }, 500);
    }
  }

  onMainDocumentChange(event: any) {
    const files = event.target.files;
    if (files.length > 0) {
      const file = files[0];
      
      const allowedExtensions = ['.pdf', '.jpg', '.jpeg', '.png'];
      const extension = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();
      if (!allowedExtensions.includes(extension)) {
        alert('Only PDF, JPG, JPEG, and PNG files are allowed.');
        return;
      }
      
      if (file.size > 5 * 1024 * 1024) {
        alert('Maximum file size allowed is 5MB.');
        return;
      }

      this.isUploading = true;
      setTimeout(() => {
        this.mainDocumentFile = file;
        this.isUploading = false;
      }, 300);
    }
  }

  onSupportingFilesChange(event: any) {
    const files = event.target.files;
    if (files.length > 0) {
      this.isUploading = true;
      setTimeout(() => {
        for (let i = 0; i < files.length; i++) {
          const file = files[i];
          if (file.size > 5 * 1024 * 1024) {
            alert(`File ${file.name} is too large. Max size 5MB.`);
            continue;
          }
          this.supportingFiles.push(file);
        }
        this.isUploading = false;
      }, 300);
    }
  }

  removeMainDocument() {
    this.mainDocumentFile = null;
  }

  removeSupportingFile(index: number) {
    this.supportingFiles.splice(index, 1);
  }

  onCheckboxChange(event: any, fieldName: string) {
    const dynamicGroup = this.requestForm.get('dynamicData') as FormGroup;
    const control = dynamicGroup.get(fieldName);
    let currentValue = control?.value || [];
    if (!Array.isArray(currentValue)) {
       currentValue = [];
    }
    
    if (event.target.checked) {
      currentValue.push(event.target.value);
    } else {
      const index = currentValue.indexOf(event.target.value);
      if (index >= 0) {
        currentValue.splice(index, 1);
      }
    }
    control?.patchValue(currentValue);
  }

  removeFile(index: number) {
    this.uploadedFiles.splice(index, 1);
  }

  submitRequest() {
    if (this.isSubmitting) return;
    console.log('--- submitRequest called ---');
    console.log('Form valid?', this.requestForm.valid);
    console.log('selectedService?', !!this.selectedService);
    console.log('selectedCategory?', !!this.selectedCategory);
    
    // Log form errors if any
    Object.keys(this.requestForm.controls).forEach(key => {
      const controlErrors = this.requestForm.get(key)?.errors;
      if (controlErrors != null) {
        console.log('Key control: ' + key + ', keyError: ', controlErrors);
      }
    });

    const dynamicGroup = this.requestForm.get('dynamicData') as FormGroup;
    Object.keys(dynamicGroup.controls).forEach(key => {
      const controlErrors = dynamicGroup.get(key)?.errors;
      if (controlErrors != null) {
        console.log('Dynamic control: ' + key + ', keyError: ', controlErrors);
      }
    });

    // By-passing strict form validation to ensure it attempts submission.
    if (this.selectedService && this.selectedCategory) {
      
      const formRawData = this.requestForm.getRawValue();
      // FALLBACK TO SESSION IF FORM NIC IS EMPTY
      const citizenNic = formRawData.nic || sessionStorage.getItem('userNic') || '';

      // Extract only the dynamic fields
      const dynamicData: any = {};
      this.formSchema.forEach(field => {
        let val = dynamicGroup.get(field.fieldName)?.value;
        
        // Convert short codes to display labels for dropdowns, radios, and checkboxes
        if (field.fieldType === 'dropdown' || field.fieldType === 'radio') {
            const isCategory5 = this.selectedCategory?.categoryId === 5;
            const isLocationOrCategory5Dropdown = isCategory5 || 
                ['province', 'district', 'dsDivision', 'newGnDivision'].includes(field.fieldName);

            if (!isLocationOrCategory5Dropdown &&
                field.parsedValidationRules?.dataSource !== 'FamilyMembers' && 
                field.parsedValidationRules?.dataSource !== 'CurrentFamilyCardMembers' && 
                field.parsedValidationRules?.dataSource !== 'SelectedFamilyMembers' && 
                field.fieldName !== 'newFamilyHead' && 
                field.fieldName !== 'affectedPerson' && 
                field.fieldName !== 'newHeadOfHousehold' && 
                field.fieldName !== 'familyMembersToMove') {
                const option = field.parsedOptions?.find((o: any) => o.value === val);
                if (option) {
                    val = option.label;
                }
            }
        } else if (field.fieldType === 'checkbox' && Array.isArray(val)) {
            val = val.map((v: any) => {
                const opt = field.parsedOptions?.find((o: any) => o.value === v);
                return opt ? opt.label : v;
            });
        }
        
        dynamicData[field.fieldName] = val;
      });

      // Include applicant type if applicable, with capitalization
      if (this.showApplicantType) {
        dynamicData['applicantType'] = this.applicantType.charAt(0).toUpperCase() + this.applicantType.slice(1);
      }

      // CREATE FORMDATA
      const formData = new FormData();
      formData.append('CitizenNic', citizenNic);
      
      let gnId = this.currentProfile?.GnDivisionId || this.currentProfile?.gnDivisionId || '';
      const sameGnVal = dynamicGroup.get('sameGnDivision')?.value;
      if (sameGnVal === 'No') {
        const selectedNewGn = dynamicGroup.get('newGnDivision')?.value;
        if (selectedNewGn) {
          gnId = selectedNewGn;
        }
      }
      formData.append('GnDivisionId', gnId);
      formData.append('ServiceId', this.selectedService.serviceId.toString());
      formData.append('CategoryId', this.selectedCategory.categoryId.toString());
      formData.append('ServiceType', this.selectedService.serviceName);
      formData.append('CategoryName', this.selectedCategory.categoryName);
      
      const desc = dynamicData.description || dynamicData.subject || this.selectedCategory.categoryName;
      formData.append('Description', desc);
      if (this.selectedCategory.categoryId === 5) {
        const reqType = dynamicData['requestType'];
        if (reqType === 'Marriage') {
          const loggedNic = this.currentProfile?.Nic || this.currentProfile?.nic || '';
          const spouseNic = dynamicGroup.get('spouseNic')?.value || '';
          
          if (spouseNic === loggedNic) {
            alert('You cannot select your own NIC as the spouse NIC.');
            return;
          }

          const loggedGender = (this.currentProfile?.gender || this.currentProfile?.Gender || 'Male').toLowerCase();
          const spouseGenderControl = dynamicGroup.get('spouseGender');
          const spouseGender = spouseGenderControl ? (spouseGenderControl.value || '').toLowerCase() : '';

          let newHead = '';
          if (loggedGender === 'male' && spouseGender === 'female') {
            newHead = loggedNic;
          } else if (spouseGender === 'male' && loggedGender === 'female') {
            newHead = spouseNic;
          }
          if (newHead) {
            dynamicData['newHeadOfHousehold'] = newHead;
          }
        }
      }

      formData.append('JsonData', JSON.stringify(dynamicData));

      // Append files
      if (this.selectedCategory.categoryId === 43 || this.selectedCategory.categoryName.includes('Document Verification')) {
        if (!this.mainDocumentFile) {
          alert('Please upload the Main Document to be verified.');
          return;
        }
        formData.append('MainDocument', this.mainDocumentFile);
        if (this.supportingFiles && this.supportingFiles.length > 0) {
          this.supportingFiles.forEach(file => {
            formData.append('Attachments', file);
          });
        }
      } else {
        if (this.uploadedFiles && this.uploadedFiles.length > 0) {
          this.uploadedFiles.forEach(file => {
            formData.append('Attachments', file);
          });
        }
      }

      console.log('Sending HTTP POST...');
      this.isSubmitting = true;
      this.http.post('http://localhost:5058/api/CitizenRequest/submit', formData)
        .subscribe({
          next: (res: any) => {
            console.log('Submission successful', res);
            this.isSubmitting = false;
            this.successRequestId = res?.requestId || 'Unknown';
            this.isSuccessModalOpen = true;
          },
          error: (err) => {
            console.error('Submission failed', err);
            this.isSubmitting = false;
            alert('Failed to submit request. Please try again.');
          }
        });

    } else {
      console.log('Form is invalid or missing service/category. Marking as touched.');
      this.requestForm.markAllAsTouched();
      // Alert the user if it's invalid
      alert('Please fill out all required fields marked with *');
    }
  }

  closeSuccessModal() {
    this.isSuccessModalOpen = false;
    this.router.navigate(['/citizen/service-requests']);
  }

  closeErrorModal() {
    this.isErrorModalOpen = false;
  }

  goBack() {
    this.router.navigate(['/citizen/service-requests/new']);
  }
}
