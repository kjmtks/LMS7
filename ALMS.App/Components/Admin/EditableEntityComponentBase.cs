﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ALMS.App.Models;
using ALMS.App.Models.Entities;
using ALMS.App.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace ALMS.App.Components.Admin
{
    [Flags]
    public enum EntityManagementPermision
    {
        DenyAll = 0x00, CanCreateNew = 0x01, CanEdit = 0x02, CanRemove = 0x04
    }

    public abstract class EditableEntityComponentBase<T> : ComponentBase
        where T : class, IEditableEntity<T>, new()
    {
        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] protected DatabaseService DB { get; set; }
        [Inject] protected NotifierService Notifier { get; set; }

        [Parameter]
        public IEnumerable<T> Collection { get; set; }
        [Parameter]
        public string CssClass { get; set; }
        [Parameter]
        public string Subject { get; set; }

        [Parameter]
        public EntityManagementPermision Permision { get; set; } = EntityManagementPermision.DenyAll;

        protected int ComponentId;
        protected T EdittingModel { get; set; } = new T();

        private ValidationMessageStore messageStore;
        protected EditContext EditContext { get; set; }
        protected EditMode Mode { get; set; } = EditMode.CreateNew;
        protected T BeforeModel = null;
        protected string ErrorMessage { get; set; }
        protected string ErrorSubject { get; set; }

        protected enum EditMode { CreateNew, Edit }

        protected Utils.Paginated<T> Pagination;

        public EditableEntityComponentBase()
        {
            ComponentId = new Random().Next();
            ResetEdittingModel();
        }
        protected virtual void SetSomeParameterToModel(T model) { }

        protected async Task OpenCreate()
        {
            if (!Permision.HasFlag(EntityManagementPermision.CanCreateNew))
            {
                ErrorSubject = "Permission Error";
                ErrorMessage = "You can not create new entity.";
                return;
            }
            EdittingModel = new T();
            SetSomeParameterToModel(EdittingModel);
            EdittingModel.PrepareModelForAddNew(DB.Context);
            Mode = EditMode.CreateNew;
            ResetEdittingModel();
            await OpenEditDialog();
        }

        protected async Task OpenEdit(T beforeModel)
        {
            if (!Permision.HasFlag(EntityManagementPermision.CanEdit))
            {
                ErrorSubject = "Permission Error";
                ErrorMessage = "You can not edit entity.";
                return;
            }
            BeforeModel = beforeModel;
            EdittingModel = beforeModel.GetEntityForEditOrRemove(DB.Context);
            if (EdittingModel == null)
            {
                ErrorSubject = "Error";
                ErrorMessage = "Not found the entity";
                return;
            }
            SetSomeParameterToModel(EdittingModel);
            EdittingModel.PrepareModelForEdit(DB.Context, beforeModel);
            Mode = EditMode.Edit;
            ResetEdittingModel(EdittingModel);
            await OpenEditDialog();
        }

        protected async Task RemoveAsync(T model)
        {
            if (!Permision.HasFlag(EntityManagementPermision.CanRemove))
            {
                ErrorSubject = "Permission Error";
                ErrorMessage = "You can not remove entity.";
                return;
            }
            if (model == null) { return; }
            var m = model.GetEntityForEditOrRemove(DB.Context);
            if (m == null)
            {
                ErrorSubject = "Error";
                ErrorMessage = "Not found the entity";
                return;
            }
            try
            {
                m.Remove(DB.Context);
            }
            catch(Exception e)
            {
                ErrorSubject = e.Message;
                ErrorMessage = e.StackTrace;
            }
            DB.Context.SaveChanges();

            await OnAfterRemoveAsync();
            await InvokeAsync(() => StateHasChanged());
            await InvokeAsync(() => Notifier.Update());
        }

        protected virtual void OnAfterCreateAndCloseDialog(T model) { }
        protected virtual void OnAfterUpdateAndCloseDialog(T model) { }
        protected virtual void OnAfterRemove() { }
        protected virtual async Task OnAfterCreateAndCloseDialogAsync(T model) { }
        protected virtual async Task OnAfterUpdateAndCloseDialogAsync(T model) { }
        protected virtual async Task OnAfterRemoveAsync() { }

        protected virtual async Task OnValidAsync(EditContext editContext)
        {
            try
            {
                if (Mode == EditMode.CreateNew)
                {
                    try
                    {
                        EdittingModel.CreateNew(DB.Context);
                    }
                    catch (Exception e)
                    {
                        ErrorSubject = e.Message;
                        ErrorMessage = e.StackTrace;
                    }
                    DB.Context.SaveChanges();
                    await CloseEditDialog();
                    OnAfterCreateAndCloseDialog(EdittingModel);
                    await OnAfterCreateAndCloseDialogAsync(EdittingModel);
                }
                if (Mode == EditMode.Edit)
                {
                    var previous = EdittingModel.GetEntityAsNoTracking(DB.Context);
                    try
                    {
                        EdittingModel.Update(DB.Context, previous);
                    }
                    catch (Exception e)
                    {
                        ErrorSubject = e.Message;
                        ErrorMessage = e.StackTrace;
                    }
                    DB.Context.SaveChanges();
                    await CloseEditDialog();
                    OnAfterUpdateAndCloseDialog(EdittingModel);
                    await OnAfterUpdateAndCloseDialogAsync(EdittingModel);
                }
                await InvokeAsync(() => StateHasChanged());
                await InvokeAsync(() => Notifier.Update());
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                Console.Error.WriteLine(e);
                await CloseEditDialog();
                return;
            }
        }

        protected void AddValidationError(string fieldName, string errorMessage)
        {
            messageStore.Add(EditContext.Field(fieldName), errorMessage);
            EditContext.NotifyValidationStateChanged();
        }
        protected async Task Submit()
        {
            var v1 = EditContext.Validate();
            var v2 = Mode == EditMode.CreateNew
                ? EdittingModel.ServerSideValidationOnCreate(DB.Context, AddValidationError)
                : EdittingModel.ServerSideValidationOnUpdate(DB.Context, AddValidationError);
            if (v1 && v2)
            {
                await OnValidAsync(EditContext);
            }
        }

        private void ResetEdittingModel(T model = null)
        {
            EditContext = new EditContext(EdittingModel);
            messageStore = new ValidationMessageStore(EditContext);
            EditContext.OnValidationRequested += (s, e) => messageStore.Clear();
            EditContext.OnFieldChanged += (s, e) => messageStore.Clear(e.FieldIdentifier);
            ErrorMessage = null;
        }

        public async Task CloseEditDialog() => await JS.InvokeVoidAsync("modalControl", $".ui.modal#edit-form-{ComponentId}", "hide");
        public async Task OpenEditDialog() => await JS.InvokeVoidAsync("modalControl", $".ui.modal#edit-form-{ComponentId}", "show");

        
    }



    public abstract class EditableChildEntityComponentBase<T, U> : EditableEntityComponentBase<T>
        where T : class, IEditableEntity<T>, IChildEntity<T, U>, new() where U : IEntity<U>
    {
        public EditableChildEntityComponentBase() : base() { }

        [Parameter]
        public U Parent { get; set; }

        protected override void SetSomeParameterToModel(T model)
        {
            model.Parent = Parent;
        }
    }
}

