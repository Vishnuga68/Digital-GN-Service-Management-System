import { Component, OnInit } from '@angular/core';
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
  
  applicantType: string = 'myself';
  isUploading: boolean = false;
  uploadedFiles: string[] = [];

  private dynamicSubscriptions: any[] = [];

  familyMembersData: any[] = [];

  currentProfile: any = null;

  constructor(private fb: FormBuilder, private router: Router, private route: ActivatedRoute, private http: HttpClient, private authService: AuthService) {
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
            try {
              f.parsedValidationRules = JSON.parse(f.validationRules);
            } catch (e) {}
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
          } else if ((f.fieldType === 'dropdown' || f.fieldType === 'checkbox') && f.parsedValidationRules?.dataSource === 'FamilyMembers') {
             // Handle dropdown options from new JSON ValidationRules configuration
             let members = this.familyMembersData;
             
             // Apply any filtering
             if (f.parsedValidationRules.filterCriteria?.excludeApplicant) {
                 // Intentionally left empty: Requirement dictates all family members must be included.
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

             f.parsedOptions = members.map(m => {
                 let label = m[displayField] || m[displayField.charAt(0).toUpperCase() + displayField.slice(1)];
                 if (displayField === 'fullName-nic') {
                     label = (m.fullName || m.FullName) + ' - ' + (m.nic || m.Nic);
                 }
                 return {
                     label: label,
                     value: m[valueField] || m[valueField.charAt(0).toUpperCase() + valueField.slice(1)]
                 };
             });
          } else if (f.parsedValidationRules?.dataSource && f.parsedValidationRules.dataSource !== 'FamilyMembers') {
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

    // Add new dynamic controls
    this.formSchema.forEach(field => {
      const validators = [];
      if (field.isRequired) validators.push(Validators.required);
      
      let initialValue = '';
      if (field.parsedValidationRules?.defaultValue) {
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

    this.evaluateVisibility();

    // Setup value change listeners for auto-population and visibility
    const visSub = dynamicGroup.valueChanges.subscribe(val => {
       this.evaluateVisibility();
    });
    this.dynamicSubscriptions.push(visSub);

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
  }

  fetchDropdownData(field: FormSchemaField, parentValue?: any) {
     let url = '';
     const source = field.parsedValidationRules?.dataSource;
     const filterField = field.parsedValidationRules?.filterField;
     
     if (source === 'province') {
        url = 'http://localhost:5058/api/Provinces';
     } else if (source === 'districts') {
        url = `http://localhost:5058/api/Districts?${filterField || 'provinceId'}=${parentValue}`;
     } else if (source === 'ds_divisions') {
        url = `http://localhost:5058/api/DsDivision?${filterField || 'districtId'}=${parentValue}`;
     } else if (source === 'gn_divisions') {
        url = `http://localhost:5058/api/GnDivision?${filterField || 'dsDivisionId'}=${parentValue}`;
     }

     if (url) {
        this.http.get<any[]>(url).subscribe(res => {
           const displayField = field.parsedValidationRules?.displayField || 'name';
           const valueField = field.parsedValidationRules?.valueField || 'id';
           field.parsedOptions = res.map(r => ({
              label: r[displayField] || r[displayField.charAt(0).toUpperCase() + displayField.slice(1)],
              value: r[valueField] || r[valueField.charAt(0).toUpperCase() + valueField.slice(1)]
           }));
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
      let isVisible = true;

      if (field.parsedValidationRules?.conditionalVisibility) {
        const rule = field.parsedValidationRules.conditionalVisibility;
        let targetValue: any;
        if (rule.field === 'applicantType') {
            targetValue = this.applicantType;
        } else {
            targetValue = dynamicGroup.get(rule.field)?.value;
        }
        
        if (rule.in && Array.isArray(rule.in)) {
           isVisible = rule.in.includes(targetValue);
        } else {
           isVisible = targetValue === rule.value || targetValue === rule.equals;
        }
      } else if (field.parsedValidationRules?.dependsOnValue !== undefined && field.parsedValidationRules?.dependsOn) {
        let targetValue: any;
        if (field.parsedValidationRules.dependsOn === 'applicantType') {
            targetValue = this.applicantType;
        } else {
            targetValue = dynamicGroup.get(field.parsedValidationRules.dependsOn)?.value;
        }
        isVisible = targetValue === field.parsedValidationRules.dependsOnValue;
      }

      // Check if visibility changed
      if (field.isVisible !== isVisible) {
        field.isVisible = isVisible;
        const control = dynamicGroup.get(field.fieldName);
        
        if (isVisible) {
          control?.enable({ emitEvent: false });
          if (field.isRequired) {
             control?.setValidators([Validators.required]);
          }
        } else {
          control?.disable({ emitEvent: false });
          control?.clearValidators();
          if (field.fieldType === 'checkbox' || field.parsedValidationRules?.multiple) {
            (control as any)?.patchValue([], { emitEvent: false });
          } else {
            (control as any)?.patchValue('', { emitEvent: false });
          }
        }
        control?.updateValueAndValidity({ emitEvent: false });
      }

      // Check for dynamic guideline mapping
      if (field.parsedValidationRules?.guidelineMap) {
         const val = dynamicGroup.get(field.fieldName)?.value;
         if (val && field.parsedValidationRules.guidelineMap[val]) {
            if (this.selectedCategory) {
               this.selectedCategory.guidelineText = field.parsedValidationRules.guidelineMap[val];
            }
         }
      }
    });
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
      alert('Please enter a NIC number first.');
      return;
    }

    const field = this.formSchema.find(f => f.fieldName === fieldName);
    if (!field) return;

    const mappings = field.parsedValidationRules?.autoPopulate || {};

    this.http.get<any>(`http://localhost:5058/api/CitizenMaster/${nicValue}`)
      .subscribe(
        res => {
          let sourceData = res.data || res.Data || res;
          
          const familyCardNumber = this.requestForm.get('familyCardNumber')?.value;
          if (familyCardNumber) {
             this.http.get<any>(`http://localhost:5058/api/FamilyCard/${familyCardNumber}/members`).subscribe(
                fcRes => {
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
                },
                err => {
                   if (Object.keys(mappings).length > 0) {
                       this.applyAutoPopulateMappings(sourceData, mappings, dynamicGroup);
                   }
                }
             );
          } else {
             if (Object.keys(mappings).length > 0) {
                 this.applyAutoPopulateMappings(sourceData, mappings, dynamicGroup);
             }
          }
        },
        err => {
          console.error('NIC lookup failed', err);
          alert('No matching citizen found for the provided NIC.');
        }
      );
  }

  get showApplicantType(): boolean {
    return this.formSchema.some(f => f.fieldType === 'applicant-type');
  }

  setApplicantType(type: string) {
    this.applicantType = type;
    this.evaluateVisibility();
  }

  onFileChange(event: any) {
    const files = event.target.files;
    if (files.length > 0) {
      this.isUploading = true;
      setTimeout(() => {
        for (let i = 0; i < files.length; i++) {
          this.uploadedFiles.push(files[i].name);
        }
        this.isUploading = false;
      }, 1000);
    }
  }

  removeFile(index: number) {
    this.uploadedFiles.splice(index, 1);
  }

  submitRequest() {
    if (this.requestForm.valid && this.selectedService && this.selectedCategory) {
      
      const formRawData = this.requestForm.getRawValue();
      const citizenNic = formRawData.nic;

      // Extract only the dynamic fields
      const dynamicData: any = {};
      const dynamicGroup = this.requestForm.get('dynamicData') as FormGroup;
      this.formSchema.forEach(field => {
        dynamicData[field.fieldName] = dynamicGroup.get(field.fieldName)?.value;
      });

      // Include applicant type if applicable
      if (this.showApplicantType) {
        dynamicData['applicantType'] = this.applicantType;
      }

      const payload = {
        serviceId: this.selectedService.serviceId,
        categoryId: this.selectedCategory.categoryId,
        citizenNic: citizenNic,
        dataJson: JSON.stringify(dynamicData)
      };

      this.http.post('http://localhost:5058/api/DynamicForm/submit', payload)
        .subscribe(
          (res: any) => {
            console.log('Submission successful', res);
            alert('Request submitted successfully!');
            this.router.navigate(['/citizen/service-requests']);
          },
          err => {
            console.error('Submission failed', err);
            alert('Failed to submit request. Please try again.');
          }
        );

    } else {
      Object.values(this.requestForm.controls).forEach(control => control.markAsTouched());
    }
  }

  goBack() {
    this.router.navigate(['/citizen/service-requests/new']);
  }
}
