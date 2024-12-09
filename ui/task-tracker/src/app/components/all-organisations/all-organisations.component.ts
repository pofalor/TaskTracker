import { Component } from '@angular/core';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { AuthService } from '../../shared/services/auth.service';
import { UserService } from '../../shared/services/user.service';
import { BaseComponent } from '../../shared/base/base.component';
import { ReactiveFormsModule } from '@angular/forms';

@Component({
    selector: 'app-all-organisations',
    imports: [ReactiveFormsModule],
    templateUrl: './all-organisations.component.html',
    styleUrl: './all-organisations.component.scss'
})
export class AllOrganisationsComponent extends BaseComponent {

constructor(
    public authService: AuthService,
    private modalService: NgbModal,
    private userService: UserService
  ) {
    super(modalService);
  }

  ngOnInit() {
    
  }
}
